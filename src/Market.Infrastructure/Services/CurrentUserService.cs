using Market.Application.Abstractions.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Services
{
    /// <summary>
    /// Service for accessing the currently authenticated user's information from HttpContext
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentUserService> _logger;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public int GetCurrentUserId()
        {
            if (!TryGetCurrentUserId(out var userId))
                throw new UnauthorizedAccessException("User is not authenticated or UserId claim not found");
            return userId;
        }

        public bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
                               ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub");
                
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var id))
                {
                    _logger.LogWarning("UserId claim not found or invalid");
                    return false;
                }

                userId = id;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user ID");
                return false;
            }
        }
    }
}
