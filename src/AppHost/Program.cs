using Microsoft.Extensions.Hosting;

#if DEBUG
// Set default values for local development
if (Environment.GetEnvironmentVariable("Parameters:DefaultRedirectUrl") == null)
{
    Environment.SetEnvironmentVariable("Parameters:DefaultRedirectUrl", "https://github.com/thgossler/MyAzUrlShortener");
}
if (Environment.GetEnvironmentVariable("Parameters:CustomDomain") == null)
{
    Environment.SetEnvironmentVariable("Parameters:CustomDomain", "");
}
#endif

var builder = DistributedApplication.CreateBuilder(args);

var isDevelopmentEnvironment = builder.Environment.IsDevelopment();

var storage = builder.AddAzureStorage("url-data");
if (isDevelopmentEnvironment)
{
    storage.RunAsEmulator();
}

var azTableClient = storage.AddTables("table-client");

var defaultRedirectUrl = builder.AddParameter("DefaultRedirectUrl");
var customDomain = builder.AddParameter("CustomDomain");

var azFuncLight = builder.AddAzureFunctionsProject<Projects.AzUrlShortener_Functions>("functions")
                            .WithReference(azTableClient)
                            .WaitFor(azTableClient)
                            .WithEnvironment("DefaultRedirectUrl", defaultRedirectUrl);

var manAPI = builder.AddProject<Projects.AzUrlShortener_Api>("api")
                        .WithReference(azTableClient)
                        .WithReference(azFuncLight)
                        .WaitFor(azTableClient)
                        .WaitFor(azFuncLight)
                        .WithEnvironment("DefaultRedirectUrl", defaultRedirectUrl)
                        .WithEnvironment("CustomDomain", customDomain);

builder.AddProject<Projects.AzUrlShortener_AdminUI>("admin-ui")
        .WithReference(manAPI);

builder.Build().Run();
