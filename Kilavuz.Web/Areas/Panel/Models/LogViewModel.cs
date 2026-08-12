using System;
using System.Collections.Generic;

namespace Kilavuz.Web.Areas.Panel.Models;

public class LogViewModel
{
    public int TopN { get; set; } = 500;
    public string? Level { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public List<AuditLogItem> AuditLogs { get; set; } = new();
    public List<ErrorLogItem> ErrorLogs { get; set; } = new();
}

public class AuditLogItem
{
    public int Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    
    // Ayrıştırılmış (Parsed) Özellikler
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IPAddress { get; set; }
    public string? RequestPath { get; set; }
}

public class ErrorLogItem
{
    public int Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? IPAddress { get; set; }
}
