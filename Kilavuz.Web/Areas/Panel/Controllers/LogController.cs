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
                Id, TimeStamp, Level, Message, Exception, Properties
            FROM AuditLogs
            WHERE 1=1";

        if (!string.IsNullOrEmpty(level))
            auditQuery += " AND Level = @Level";
            
        if (startDate.HasValue)
            auditQuery += " AND TimeStamp >= @StartDate";
            
        if (endDate.HasValue)
            auditQuery += " AND TimeStamp <= @EndDate";

        auditQuery += " ORDER BY TimeStamp DESC";

        var rawAuditLogs = await connection.QueryAsync<dynamic>(auditQuery, new { TopN = topN, Level = level, StartDate = startDate, EndDate = endDate });

        foreach (var row in rawAuditLogs)
        {
            var item = new AuditLogItem
            {
                Id = row.Id,
                TimeStamp = row.TimeStamp,
                Level = row.Level ?? string.Empty,
                Message = row.Message ?? string.Empty,
                Exception = row.Exception
            };

            // Parse Properties XML
            string propertiesXml = row.Properties;
            if (!string.IsNullOrWhiteSpace(propertiesXml))
            {
                try
                {
                    var xml = XElement.Parse(propertiesXml);
                    item.UserId = xml.Elements("property").FirstOrDefault(e => (string?)e.Attribute("key") == "UserId")?.Value;
                    item.UserName = xml.Elements("property").FirstOrDefault(e => (string?)e.Attribute("key") == "UserName")?.Value;
                    item.IPAddress = xml.Elements("property").FirstOrDefault(e => (string?)e.Attribute("key") == "IPAddress")?.Value;
                    item.RequestPath = xml.Elements("property").FirstOrDefault(e => (string?)e.Attribute("key") == "RequestPath")?.Value;
                }
                catch
                {
                    // XML parse hatası yoksayılır
                }
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
