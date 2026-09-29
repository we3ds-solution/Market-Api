using System.Net;
using System.Text.Json;
using Market.API.Services;

namespace Market.API.Middleware
{
    /// <summary>
    /// Centralized exception handling middleware for all errors.
    /// Catches unhandled exceptions and delegates mapping to IExceptionMapper (SRP).
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IExceptionMapper _exceptionMapper;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IExceptionMapper exceptionMapper)
        {
            _next = next;
            _logger = logger;
            _exceptionMapper = exceptionMapper;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Map exception to HTTP response using the mapper service
            _exceptionMapper.MapException(context, exception);

            // Get error response and write to response body
            var errorResponse = _exceptionMapper.GetErrorResponse(exception);
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }

    /// <summary>
    /// Error response model
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
