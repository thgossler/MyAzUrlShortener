using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AzUrlShortener.AdminUI.Services;

// Custom Auth State Provider based on the ServerAuthenticationStateProvider
public class CustomAuthStateProvider : ServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CustomAuthStateProvider> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomAuthStateProvider(
        IHttpContextAccessor httpContextAccessor,
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
        _logger = loggerFactory.CreateLogger<CustomAuthStateProvider>();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }
}
