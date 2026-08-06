using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Kilavuz.Web.Infrastructure.Logging
{
    public class AuditLogFilter : ILogEventFilter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _panelRoutePrefix;

        public AuditLogFilter(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _panelRoutePrefix = configuration.GetValue<string>("Logging:PanelRoutePrefix") ?? "/Panel";
        }

        public bool IsEnabled(LogEvent logEvent)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                // If there's no HTTP context, it's a background task or startup log.
                // We can choose to log these or not. Let's log them to be safe.
                return true;
            }

            var requestPath = context.Request.Path.Value;
            
            // Log only if the request path starts with the Panel route prefix (case insensitive)
            if (!string.IsNullOrEmpty(requestPath) && requestPath.StartsWith(_panelRoutePrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Also check if the RequestPath property is set in the log event directly
            if (logEvent.Properties.TryGetValue("RequestPath", out var pathValue) && pathValue is ScalarValue scalar)
            {
                var pathStr = scalar.Value?.ToString();
                if (!string.IsNullOrEmpty(pathStr) && pathStr.StartsWith(_panelRoutePrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
