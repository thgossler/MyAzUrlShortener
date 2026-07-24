using System.Reflection;

using Azure.Core.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Ensure assembly resolution for System.Memory.Data in container environments
AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
    var assemblyName = new AssemblyName(args.Name);
    if (assemblyName.Name == "System.Memory.Data") {
        // Try to load the available version
        try {
            return Assembly.LoadFrom("System.Memory.Data.dll");
        }
        catch {
            // Fallback to any loaded version
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "System.Memory.Data");
        }
    }
    return null;
};

builder.AddServiceDefaults();

builder.AddAzureTableServiceClient("table-client");

using var listener = AzureEventSourceListener.CreateConsoleLogger();

builder.Services.AddTransient<ILogger>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return loggerFactory.CreateLogger("AzUrlShortenerLogger");
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.MapShortenerEnpoints();

app.Run();

