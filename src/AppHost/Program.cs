using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var isDevelopmentEnvironment = builder.Environment.IsDevelopment();

var storage = builder.AddAzureStorage("url-data");
if (isDevelopmentEnvironment)
{
    storage.RunAsEmulator();
}

var azTableClient = storage.AddTables("table-client");

if (string.IsNullOrWhiteSpace(builder.Configuration["Parameters:DefaultRedirectUrl"]))
{
    builder.Configuration["Parameters:DefaultRedirectUrl"] = "https://github.com/thgossler/MyAzUrlShortener";
}
var defaultRedirectUrl = builder.AddParameter("DefaultRedirectUrl");

var azFuncLight = builder.AddAzureFunctionsProject<Projects.AzUrlShortener_FunctionsLight>("azfunc-light")
                            .WithReference(azTableClient)
                            .WaitFor(azTableClient)
                            .WithEnvironment("DefaultRedirectUrl", defaultRedirectUrl)
                            .WithExternalHttpEndpoints();

var manAPI = builder.AddProject<Projects.AzUrlShortener_Api>("api")
						.WithReference(azTableClient)
                        .WithReference(azFuncLight)
						.WaitFor(azTableClient)
                        .WaitFor(azFuncLight)
                        .WithEnvironment("DefaultRedirectUrl", defaultRedirectUrl)
                        .WithEnvironment(env =>
                            {
                                if (isDevelopmentEnvironment)
                                {
                                    // Locally, we only get an http for the Azure function, in Azure it can be configured to redirect to https
                                    var azFuncLightEndpoint = azFuncLight.GetEndpoint("http");
                                    var url = azFuncLightEndpoint.Url;
                                    env.EnvironmentVariables["CustomDomain"] = url;
                                }
                            });
						//.WithExternalHttpEndpoints(); // If you want to access the API directly

builder.AddProject<Projects.AzUrlShortener_TinyBlazorAdmin>("admin")
		.WithExternalHttpEndpoints()
		.WithReference(manAPI);

builder.Build().Run();
