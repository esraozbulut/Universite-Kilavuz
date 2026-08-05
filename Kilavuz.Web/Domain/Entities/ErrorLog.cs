using System;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class ErrorLog : IEntity
{
    public int Id { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? IPAddress { get; set; }
}
