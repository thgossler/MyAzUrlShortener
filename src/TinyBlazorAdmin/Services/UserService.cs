using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace AzUrlShortener.TinyBlazorAdmin.Services;

public class UserService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(AuthenticationStateProvider authStateProvider, IHttpContextAccessor httpContextAccessor)
    {
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GetUserPrincipalNameAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

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

        // Last resort: use name
        var nameClaim = user.FindFirst(ClaimTypes.Name);
        if (nameClaim != null)
            return normalize(nameClaim.Value);

        return string.Empty;
    }

    public async Task<bool> IsAdminAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        return user.IsInRole("Admin");
    }

    public async Task<bool> IsUserAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        return user.IsInRole("User") || user.IsInRole("Admin");
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
