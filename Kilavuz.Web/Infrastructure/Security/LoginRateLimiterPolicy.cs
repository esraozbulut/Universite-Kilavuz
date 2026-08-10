using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System;
using System.Threading.RateLimiting;

namespace Kilavuz.Web.Infrastructure.Security;

public class LoginRateLimiterPolicy : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; } =
        (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            return new ValueTask();
        };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString();
        string username = httpContext.Items["LoginUsername"]?.ToString() ?? string.Empty;

        string partitionKey = string.IsNullOrEmpty(username) ? ip : $"{ip}:{username}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    }
}
