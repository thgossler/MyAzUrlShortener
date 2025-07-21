namespace AzUrlShortener.Core.Messages;

/// <summary>
/// Response for import operations containing statistics about the import
/// </summary>
public class ImportResponse
{
    /// <summary>
    /// Number of UrlsDetails records successfully imported
    /// </summary>
    public int UrlsImported { get; set; }

    /// <summary>
    /// Number of ClickStats records successfully imported
    /// </summary>
    public int ClickStatsImported { get; set; }

    /// <summary>
    /// Number of records that were skipped (e.g., already exist and overwrite is false)
    /// </summary>
    public int RecordsSkipped { get; set; }

    /// <summary>
    /// Number of records that failed to import
    /// </summary>
    public int RecordsFailed { get; set; }

    /// <summary>
    /// List of error messages encountered during import
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether the import operation was successful overall
    /// </summary>
    public bool Success => RecordsFailed == 0;

    /// <summary>
    /// Total number of records processed
    /// </summary>
    public int TotalProcessed => UrlsImported + ClickStatsImported + RecordsSkipped + RecordsFailed;
}
