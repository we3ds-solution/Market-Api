namespace Market.Application.Abstractions.Services
{
    /// <summary>
    /// Service for accessing the currently authenticated user's information
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Get the current authenticated user's ID
        /// </summary>
        /// <returns>The user ID from authentication claims</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated or UserId claim is not found</exception>
        int GetCurrentUserId();

        /// <summary>
        /// Try to get the current authenticated user's ID
        /// </summary>
        /// <param name="userId">The user ID if found</param>
        /// <returns>True if user ID was found, false otherwise</returns>
        bool TryGetCurrentUserId(out int userId);
    }
}
