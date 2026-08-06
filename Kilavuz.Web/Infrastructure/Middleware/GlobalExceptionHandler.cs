using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Infrastructure.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly string _panelRoutePrefix;

        public GlobalExceptionHandler(IConfiguration configuration)
        {
            _panelRoutePrefix = configuration.GetValue<string>("Logging:PanelRoutePrefix") ?? "/Panel";
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var requestPath = httpContext.Request.Path.Value;

            // Option A: Log to ErrorLogs only if it's a UI error (not starting with Panel route)
            if (string.IsNullOrEmpty(requestPath) || !requestPath.StartsWith(_panelRoutePrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Resolve IErrorLogService from the current request scope
                var errorLogService = httpContext.RequestServices.GetRequiredService<IErrorLogService>();

                var errorLog = new ErrorLog
                {
                    TimeStamp = DateTime.UtcNow,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    RequestPath = requestPath,
                    IPAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                };

                await errorLogService.LogErrorAsync(errorLog);
            }

            // Return false to allow other handlers (like DeveloperExceptionPage or UseExceptionHandler route) to process the response
            return false;
        }
    }
}
