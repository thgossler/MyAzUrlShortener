using Azure;
using Azure.Data.Tables;

namespace AzUrlShortener.Core.Domain;

public class NextId : ITableEntity
{
    public int Id { get; set; }
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
