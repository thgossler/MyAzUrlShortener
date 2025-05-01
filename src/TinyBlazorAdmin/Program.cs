using AzUrlShortener.TinyBlazorAdmin;
using AzUrlShortener.TinyBlazorAdmin.Components;
using AzUrlShortener.TinyBlazorAdmin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Components.Tooltip;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                     .AddJsonFile("appsettings.local.json", optional: true);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddHttpClient<UrlManagerClient>(client => 
            {
                client.BaseAddress = new Uri("https+http://api");
            });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Entra";
})
.AddOpenIdConnect("Entra", options =>
{
    var tenantId = builder.Configuration["Authentication:Entra:TenantId"];
    var clientId = builder.Configuration["Authentication:Entra:ClientId"];
    var clientSecret = builder.Configuration["Authentication:Entra:ClientSecret"];

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.UseTokenLifetime = true; 
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.Scope.Add("openid");
    options.CallbackPath = "/signin-oidc";
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
            {
                context.ProtocolMessage.Prompt = "select_account";
            }
            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validated and saved to authentication properties");
            return Task.CompletedTask;
        },

        OnAuthorizationCodeReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Authorization code received");
            return Task.CompletedTask;
        }
    };

    var applicationUrl = builder.Configuration["ApplicationUrl"] ?? "https://localhost:7203";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.SignedOutRedirectUri = applicationUrl;
})
.AddCookie(options =>
 {
     options.ExpireTimeSpan = TimeSpan.FromHours(1);
     options.SlidingExpiration = true;
     options.Cookie.Name = "AzUrlShortener.Auth";
     options.Cookie.HttpOnly = true;
     options.Cookie.SameSite = SameSiteMode.Lax;
     options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
 });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<UserService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddScoped<ITooltipService, TooltipService>();

builder.Services.AddControllers();

builder.Services.AddBlazorBootstrap();

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapControllers();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
