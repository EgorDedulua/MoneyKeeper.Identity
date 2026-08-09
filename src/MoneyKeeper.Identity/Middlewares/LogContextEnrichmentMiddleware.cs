using Serilog.Context;
using System.Diagnostics;

namespace MoneyKeeper.Identity.Middlewares
{
    public class LogContextEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public LogContextEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            using (LogContext.PushProperty("TraceId", Activity.Current?.Id ?? httpContext.TraceIdentifier))
            {
                await _next(httpContext);
            }
        }
    }
}
