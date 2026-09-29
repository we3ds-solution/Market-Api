namespace Market.API.Middleware
{
    /// <summary>
    /// Extension methods for custom middleware registration.
    /// Provides fluent API for adding cross-cutting concerns to the HTTP pipeline.
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Add custom middleware in the correct order for cross-cutting concerns.
        /// Order matters:
        /// 1. CorrelationId (add ID to all logs)
        /// 2. RequestLogging (log incoming requests)
        /// 3. ValidationMiddleware (validate request before business logic)
        /// 4. ExceptionHandling (catch all exceptions)
        /// 5. SecurityHeaders (add headers to responses)
        /// </summary>
        public static WebApplication UseCustomMiddleware(this WebApplication app)
        {
            // Add correlation ID middleware
            app.UseMiddleware<CorrelationIdMiddleware>();

            // Add request logging middleware
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Add validation middleware
            app.UseMiddleware<ValidationMiddleware>();

            // Add exception handling middleware (must be near top to catch all exceptions)
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Add security headers middleware
            app.UseMiddleware<SecurityHeadersMiddleware>();

            return app;
        }
    }
}
