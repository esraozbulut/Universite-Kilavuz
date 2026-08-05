using System;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class LoginAttempt : IEntity
{
    public int Id { get; set; }
    public string UserNameAttempted { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
