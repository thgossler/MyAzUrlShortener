using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using System.Reflection;

var builder = FunctionsApplication.CreateBuilder(args);

// Ensure assembly resolution for System.Memory.Data in container environments
AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    var assemblyName = new AssemblyName(args.Name);
    if (assemblyName.Name == "System.Memory.Data")
    {
        // Try to load the available version
        try
        {
            return Assembly.LoadFrom("System.Memory.Data.dll");
        }
        catch
        {
            // Fallback to any loaded version
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "System.Memory.Data");
        }
    }
    return null;
};

builder.AddAzureTableClient("table-client");

builder.ConfigureFunctionsWebApplication();

builder.EnableMcpToolMetadata();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
