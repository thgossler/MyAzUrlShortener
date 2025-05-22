using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddAzureTableClient("table-client");

builder.ConfigureFunctionsWebApplication();

builder.EnableMcpToolMetadata();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
