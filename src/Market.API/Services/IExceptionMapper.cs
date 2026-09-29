namespace Market.API.Services
{
    /// <summary>
    /// Abstraction for mapping exceptions to HTTP responses.
    /// Separates exception mapping concerns from middleware.
    /// </summary>
    public interface IExceptionMapper
    {
        /// <summary>
        /// Map exception to HTTP context response (status code, headers).
        /// Caller is responsible for writing response body.
        /// </summary>
        void MapException(HttpContext context, Exception exception);

        /// <summary>
        /// Get error response DTO for an exception.
        /// Can be used independently of HTTP context.
        /// </summary>
        ErrorResponse GetErrorResponse(Exception exception);
    }
}
