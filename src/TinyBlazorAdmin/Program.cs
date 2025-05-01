using AzUrlShortener.TinyBlazorAdmin;
using AzUrlShortener.TinyBlazorAdmin.Components;
using AzUrlShortener.TinyBlazorAdmin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Components.Tooltip;

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

// Add services to the container.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Entra";
})
.AddCookie()
.AddOpenIdConnect("Entra", options =>
{
    var tenantId = builder.Configuration["Authentication:Entra:TenantId"];
    var clientId = builder.Configuration["Authentication:Entra:ClientId"];
    var clientSecret = builder.Configuration["Authentication:Entra:ClientSecret"];

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;  // Add this line
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.Scope.Add("openid");
    options.CallbackPath = "/signin-oidc";
});

builder.Services.AddAuthorization(options =>
{
    // By default users need authentication
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Define policies for Admin and User roles
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

// Add the HttpContextAccessor for getting the current user
builder.Services.AddHttpContextAccessor();

// Add authorization state provider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

builder.Services.AddScoped<UserService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddScoped<ITooltipService, TooltipService>();

//Blazor Bootstrap service
builder.Services.AddBlazorBootstrap();

var app = builder.Build();
app.MapDefaultEndpoints();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
