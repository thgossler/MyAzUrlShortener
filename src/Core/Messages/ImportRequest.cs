namespace AzUrlShortener.Core.Messages;

/// <summary>
/// Request for importing UrlsDetails and ClickStats entities from another storage account
/// </summary>
public class ImportRequest
{
    /// <summary>
    /// Connection string to the source Azure Table Storage account
    /// </summary>
    public string SourceConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include archived URLs in the import (default: false)
    /// </summary>
    public bool IncludeArchived { get; set; } = false;

    /// <summary>
    /// Whether to overwrite existing URLs if they already exist (default: false)
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;
}
