using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using AzUrlShortener.Core.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AzUrlShortener.Functions
{
    public class BackupData
    {
        private readonly ILogger _logger;
        private readonly TableServiceClient _sourceTableServiceClient;

        public BackupData(ILoggerFactory loggerFactory, TableServiceClient sourceTableServiceClient)
        {
            _logger = loggerFactory.CreateLogger<BackupData>();
            _sourceTableServiceClient = sourceTableServiceClient;
        }

        [Function("BackupTableStorage")]
        public async Task Run(
            [TimerTrigger("%BackupSchedule%"
#if DEBUG
                , RunOnStartup = true
#endif
                )] TimerInfo myTimer
            )
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            string currentUtcTimestamp = DateTime.UtcNow.ToString("yyyy/MM/dd/HHmmss");

            var backupConnectionString = Environment.GetEnvironmentVariable("BackupBlobStorageConnectionString");
            if (string.IsNullOrEmpty(backupConnectionString))
            {
                _logger.LogError("BackupBlobStorageConnectionString is not set. Skipping backup.");
                return;
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(backupConnectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient("data");
                await blobContainerClient.CreateIfNotExistsAsync();

                // Backup UrlsDetails table
                await BackupTableAsync<ShortUrlEntity>(
                    _sourceTableServiceClient,
                    blobContainerClient,
                    "UrlsDetails",
                    $"{currentUtcTimestamp}-urldatabackup.json",
                    "UrlsDetails"
                );

                // Backup ClickStats table
                await BackupTableAsync<ClickStatsEntity>(
                    _sourceTableServiceClient,
                    blobContainerClient,
                    "ClickStats",
                    $"{currentUtcTimestamp}-urldataclickstatsbackup.json",
                    "ClickStats"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during overall backup process: {ex.Message}", ex);
            }
        }

        private async Task BackupTableAsync<T>(
            TableServiceClient sourceTableServiceClient,
            BlobContainerClient blobContainerClient,
            string tableName,
            string blobName,
            string logTableFriendlyName) where T : class, ITableEntity, new()
        {
            _logger.LogInformation($"Starting data export for {logTableFriendlyName} to JSON blob: {blobName}...");

            try
            {
                var sourceTableClient = sourceTableServiceClient.GetTableClient(tableName);

                // Check if the table exists first
                var entities = new List<T>();

                try
                {
                    // Try to get the first entity to check if table exists and has data
                    await foreach (var entity in sourceTableClient.QueryAsync<T>(maxPerPage: 1))
                    {
                        entities.Add(entity);
                        break; // We just want to check if there's at least one entity
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    _logger.LogInformation($"Table {logTableFriendlyName} does not exist. Skipping backup for this table.");
                    return;
                }

                // If we didn't get any entities from the test query, the table is empty
                if (entities.Count == 0)
                {
                    _logger.LogInformation($"No entities found in {logTableFriendlyName} table. Skipping backup for this table.");
                    return;
                }

                // Clear the test entities and get all entities for backup
                entities.Clear();
                await foreach (var entity in sourceTableClient.QueryAsync<T>())
                {
                    entities.Add(entity);
                }

                var jsonContent = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }
                
                _logger.LogInformation($"Backup completed for {logTableFriendlyName}. {entities.Count} entities backed up to blob: {blobContainerClient.Uri}/{blobName}");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation($"Table {logTableFriendlyName} does not exist. Skipping backup for this table.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during backup for {logTableFriendlyName} table: {ex.Message}", ex);
            }
        }
    }
}
