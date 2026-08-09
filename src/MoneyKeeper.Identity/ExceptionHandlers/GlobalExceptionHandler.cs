using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MoneyKeeper.Identity.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, IWebHostEnvironment environment
            , ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _environment = environment;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException)
            {
                if (httpContext.RequestAborted.IsCancellationRequested)
                {
                    _logger.LogInformation("Запрос к {Path} был отменён клиентом", httpContext.Request.Path);
                }
                else
                {
                    _logger.LogWarning(exception, "Таймаут при выполнении запроса к {Path}", httpContext.Request.Path);
                }
                return true;
            }

            _logger.LogError(exception, "Произошло необработанное исключение: {Message}", exception.Message);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal server error",
                    Detail = _environment.IsDevelopment() ? exception.ToString() : "Unexpected error"
                }
            });
        }
    }
}
