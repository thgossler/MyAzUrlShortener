using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace AzUrlShortener.AdminUI.Services;

public class UserService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public UserService(AuthenticationStateProvider authStateProvider, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<string> GetUserPrincipalNameAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return string.Empty;
        }

        var normalize = (string val) => {
            if (string.IsNullOrWhiteSpace(val))
                return string.Empty;
            return val.ToLowerInvariant().Trim();
        };

        // Try to get the UPN claim
        var upnClaim = user.FindFirst(ClaimTypes.Upn);
        if (upnClaim != null)
            return normalize(upnClaim.Value);

        // Fall back to email if UPN is not available
        var emailClaim = user.FindFirst(ClaimTypes.Email);
        if (emailClaim != null)
            return normalize(emailClaim.Value);

        // Last resort: preferred_username
        var nameClaim = user.FindFirst("preferred_username");
        if (nameClaim != null)
            return normalize(nameClaim.Value);

        return string.Empty;
    }

    public async Task<bool> IsAdminAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.IsInRole("Admin");
    }

    public async Task<bool> IsUserAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        return user.IsInRole("User") || user.IsInRole("Admin");
    }

    public bool CanNormalUsersViewAllRecords()
    {
        bool canViewAll = false;
        bool.TryParse(_configuration["AllowRegularUsersToViewAllRecords"], out canViewAll);
        return canViewAll;
    }

    public bool CanNormalUsersArchiveRecords()
    {
        bool canArchive = false;
        bool.TryParse(_configuration["AllowRegularUsersToArchiveRecords"], out canArchive);
        return canArchive;
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync();
        }
    }
}
