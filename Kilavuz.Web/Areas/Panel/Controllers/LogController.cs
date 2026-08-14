using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Areas.Panel.Models;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize(Policy = "SuperAdminOnly")]
public class LogController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LogController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int topN = 500, string? level = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Güvenlik amaçlı maksimum limit
        if (topN > 5000) topN = 5000;
        if (topN < 10) topN = 100;

        using var connection = _connectionFactory.CreateConnection();

        var model = new LogViewModel
        {
            TopN = topN,
            Level = level,
            StartDate = startDate,
            EndDate = endDate
        };

        // 1. Audit Logs (Serilog MSSQL Sink)
        var auditQuery = @"
            SELECT TOP (@TopN)
                A.Id, A.TimeStamp, A.Level, A.Message, A.Exception, A.UserId, A.IPAddress, A.RequestPath, U.UserName, A.Properties
            FROM AuditLogs A
            LEFT JOIN Users U ON A.UserId = U.Id
            WHERE 1=1";

        if (!string.IsNullOrEmpty(level))
            auditQuery += " AND A.Level = @Level";
            
        if (startDate.HasValue)
            auditQuery += " AND A.TimeStamp >= @StartDate";
            
        if (endDate.HasValue)
            auditQuery += " AND A.TimeStamp <= @EndDate";

        auditQuery += " ORDER BY A.TimeStamp DESC";

        var rawAuditLogs = await connection.QueryAsync<dynamic>(auditQuery, new { TopN = topN, Level = level, StartDate = startDate, EndDate = endDate });

        foreach (var row in rawAuditLogs)
        {
            var item = new AuditLogItem
            {
                Id = row.Id,
                TimeStamp = row.TimeStamp,
                Level = row.Level ?? string.Empty,
                Message = row.Message ?? string.Empty,
                Exception = row.Exception,
                UserId = row.UserId?.ToString(),
                UserName = row.UserName?.ToString(),
                IPAddress = row.IPAddress?.ToString(),
                RequestPath = row.RequestPath?.ToString()
            };

            // Eğer Login gibi UserId'nin olmadığı özel işlemler varsa UserName'i Serilog özelliklerinden (XML) çekmeyi dene
            if (string.IsNullOrEmpty(item.UserName) && !string.IsNullOrWhiteSpace((string?)row.Properties))
            {
                try
                {
                    var xml = XElement.Parse((string)row.Properties);
                    item.UserName = xml.Elements("property").FirstOrDefault(e => (string?)e.Attribute("key") == "Username")?.Value;
                }
                catch { }
            }

            model.AuditLogs.Add(item);
        }

        // 2. Error Logs (UI Error Log Service)
        var errorQuery = @"
            SELECT TOP (@TopN)
                Id, TimeStamp, Message, StackTrace, RequestPath, IPAddress
            FROM ErrorLogs
            WHERE 1=1";

        if (startDate.HasValue)
            errorQuery += " AND TimeStamp >= @StartDate";
            
        if (endDate.HasValue)
            errorQuery += " AND TimeStamp <= @EndDate";

        errorQuery += " ORDER BY TimeStamp DESC";

        var errorLogs = await connection.QueryAsync<ErrorLogItem>(errorQuery, new { TopN = topN, StartDate = startDate, EndDate = endDate });
        model.ErrorLogs = errorLogs.ToList();

        return View(model);
    }
}
