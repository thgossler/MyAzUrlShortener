using Azure;
using Azure.Data.Tables;

namespace AzUrlShortener.Core.Domain;

public class ClickStatsEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string ClickDatetime { get; set; }

    public ClickStatsEntity() {
        PartitionKey = "default";
        RowKey = Guid.NewGuid().ToString();
        ClickDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }

    public ClickStatsEntity(string vanity) : this()
    {
        PartitionKey = vanity.ToLowerInvariant();
    }
}
