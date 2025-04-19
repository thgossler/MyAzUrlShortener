using Azure;
using Azure.Data.Tables;

namespace Cloud5mins.ShortenerTools.Core.Domain;

public class ClickStatsEntity : ITableEntity
{
    public string Datetime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public ClickStatsEntity() { }

    public ClickStatsEntity(string vanity)
    {
        PartitionKey = vanity.ToLowerInvariant();
    }
}
