using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AzUrlShortener.TinyBlazorAdmin.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        [HttpGet("switch-account")]
        public async Task<IActionResult> SwitchAccount()
        {
            try
            {
                // First, sign out locally to clear the current session
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("Local sign-out completed");

                // Create auth properties with select_account prompt
                var props = new AuthenticationProperties
                {
                    RedirectUri = "/", // Redirect to homepage after authentication
                    IsPersistent = true // Keep the user logged in
                };
                props.Parameters.Add("prompt", "select_account");

                return Challenge(props, "Entra");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during account switching");
                return Redirect("/");
            }
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Sign out of local authentication
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Sign out of Entra
                return SignOut(
                    new AuthenticationProperties { RedirectUri = "/" },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    "Entra"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout process");
                return Redirect("/");
            }
        }
    }
}
