using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

var builder = DistributedApplication.CreateBuilder(args);

var isDevelopmentEnvironment = builder.Environment.IsDevelopment();

// Read configuration from appsettings.json and environment variables in the correct order
builder.Configuration.AddJsonFile("appsettings.json", optional: false)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                     .AddJsonFile("appsettings.local.json", optional: true)
                     .AddEnvironmentVariables();

// Read parameter defaults from configuration and request missing values
var processParameterInput = (string name, bool mayBeEmpty = false, string defaultValue = null) =>
{
    var settingValue = builder.Configuration[name];
    if (!string.IsNullOrWhiteSpace(settingValue) || mayBeEmpty)
    {
        return builder.AddParameter(name, string.IsNullOrEmpty(settingValue) ? (defaultValue != null ? defaultValue : settingValue) : settingValue, !Regex.IsMatch(name, "(Secret|Key|Password)", RegexOptions.IgnoreCase));
    }
    else
    {
        return builder.AddParameter(name);
    }
};
var defaultRedirectUrl = processParameterInput("DefaultRedirectUrl");
var customDomain = processParameterInput("CustomDomain", true, "");
var entraTenantId = processParameterInput("UserAuthEntraTenantId");
var entraClientAppId = processParameterInput("UserAuthEntraClientAppId");

// TODO: Use Key Vault for secrets
var entraClientAppSecret = processParameterInput("UserAuthEntraClientAppSecret");

// TODO: Use Key Vault for connection strings
var backupBlobStorageConnectionString = processParameterInput("BackupBlobStorageConnectionString", true, "");

var backupSchedule = processParameterInput("BackupSchedule", true, "0 */1 * * *");
var importTableStorageConnectionString = processParameterInput("ImportTableStorageConnectionString", true, "");
var allowRegularUsersToViewAllRecords = processParameterInput("AllowRegularUsersToViewAllRecords", true, "true");
var allowRegularUsersToArchiveRecords = processParameterInput("AllowRegularUsersToArchiveRecords", true, "false");
var primaryColor = processParameterInput("PrimaryColor", true, "");
var headerLogoUrl = processParameterInput("LogoUrl", true, "");
var footerLeftHtml = processParameterInput("FooterLeftHtml", true, "");
var footerCenterHtml = processParameterInput("FooterCenterHtml", true, "");
var footerRightHtml = processParameterInput("FooterRightHtml", true, "");

// Storage
var storage = builder.AddAzureStorage("url-data");
if (isDevelopmentEnvironment)
{
    storage.RunAsEmulator();
}
var azTableClient = storage.AddTables("table-client");

// Redirect Function App
var functionApp = builder.AddAzureFunctionsProject<Projects.AzUrlShortener_Functions>("functions")
                            .WithReference(azTableClient)
                            .WaitFor(azTableClient)
                            .WithEnvironment("DefaultRedirectUrl", defaultRedirectUrl)
                            .WithEnvironment("BackupSchedule", backupSchedule)
                            .WithEnvironment("BackupBlobStorageConnectionString", backupBlobStorageConnectionString)
                            .WithExternalHttpEndpoints();

// API Web Service
var apiService = builder.AddProject<Projects.AzUrlShortener_Api>("api")
                        .WithReference(azTableClient)
                        .WithReference(functionApp)
                        .WaitFor(azTableClient)
                        .WaitFor(functionApp)
                        .WithEnvironment("DefaultRedirectUrl", defaultRedirectUrl)
                        .WithEnvironment("CustomDomain", customDomain)
                        .WithExternalHttpEndpoints();

// Admin UI Web App
builder.AddProject<Projects.AzUrlShortener_AdminUI>("admin-ui")
        .WithReference(apiService)
        .WithExternalHttpEndpoints()
        .WithEnvironment("UserAuthEntraTenantId", entraTenantId)
        .WithEnvironment("UserAuthEntraClientAppId", entraClientAppId)
        .WithEnvironment("UserAuthEntraClientAppSecret", entraClientAppSecret)
        .WithEnvironment("PrimaryColor", primaryColor)
        .WithEnvironment("LogoUrl", headerLogoUrl)
        .WithEnvironment("FooterLeftHtml", footerLeftHtml)
        .WithEnvironment("FooterCenterHtml", footerCenterHtml)
        .WithEnvironment("FooterRightHtml", footerRightHtml)
        .WithEnvironment("AllowRegularUsersToViewAllRecords", allowRegularUsersToViewAllRecords)
        .WithEnvironment("AllowRegularUsersToArchiveRecords", allowRegularUsersToArchiveRecords)
        .WithEnvironment("ImportTableStorageConnectionString", importTableStorageConnectionString);

// Build and run the application
builder.Build().Run();
