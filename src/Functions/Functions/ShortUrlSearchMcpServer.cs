using Azure;
using Azure.Data.Tables;
using AzUrlShortener.Core.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzUrlShortener.Functions
{
    /// <summary>
    /// MCP server for searching short URLs directly from Azure Table Storage, optimized for large datasets.
    /// Supports server-side paging.
    /// </summary>
    public class ShortUrlSearchMcpServer
    {
        private readonly ILogger _logger;
        private readonly TableServiceClient _tblClient;

        /// <summary>
        /// Constructs the ShortUrlSearch MCP server with DI-provided TableServiceClient.
        /// </summary>
        public ShortUrlSearchMcpServer(ILoggerFactory loggerFactory, TableServiceClient tblClient)
        {
            _logger = loggerFactory.CreateLogger<ShortUrlSearchMcpServer>();
            _tblClient = tblClient;
        }

        /// <summary>
        /// Searches for short URL records based on provided parameters.
        /// </summary>
        /// <param name="context">The tool invocation context for MCP integration.</param>
        /// <param name="vanity">The exact vanity string to search for (case-insensitive). If provided, performs a direct lookup.</param>
        /// <param name="searchTerm">A full-text search term applied to vanity, title, and URL fields (case-insensitive).</param>
        /// <param name="includeArchived">If true, includes archived URLs in the results; otherwise, excludes them.</param>
        /// <param name="pageSize">The number of results to return per page (default is 100).</param>
        /// <param name="page">The 1-based page number of results to return.</param>
        /// <param name="vanityStartsWith">A prefix for the vanity string to search for (case-insensitive). If provided, performs a prefix scan.</param>
        /// <returns>
        /// A <see cref="McpResponseData"/> containing paged search results, total count, and paging metadata.
        /// </returns>
        [Function(nameof(GetShortUrlRecords))]
        public async Task<McpResponseData> GetShortUrlRecords(
            [McpToolTrigger("search_shorturl_records", "Searches for short URL records based on provided parameters.")] ToolInvocationContext context,
            [McpToolProperty("vanity", "string", "The exact vanity string to search for (case-insensitive). If provided, performs a direct lookup.")] string vanity = null,
            [McpToolProperty("searchTerm", "string", "A full-text search term applied to vanity, title, and URL fields (case-insensitive).")] string searchTerm = null,
            [McpToolProperty("includeArchived", "string", "If true, includes archived URLs in the results; otherwise, excludes them.")] string includeArchived = "false",
            [McpToolProperty("pageSize", "string", "The number of results to return per page (default is 100).")] string pageSize = "100",
            [McpToolProperty("page", "string", "The 1-based page number of results to return.")] string page = "1",
            [McpToolProperty("vanityStartsWith", "string", "Filters results to only those where the vanity name starts with the specified string (case-insensitive). Use for prefix searches.")] string vanityStartsWith = null)
        {
            _logger.LogInformation("Processing GetShortUrlRecords request (optimized for large datasets)");

            // Parse string parameters to their actual types
            bool includeArchivedBool = bool.TryParse(includeArchived, out var b) && b;
            int pageSizeInt = int.TryParse(pageSize, out var ps) ? ps : 100;
            int pageInt = int.TryParse(page, out var p) ? p : 1;

            var table = _tblClient.GetTableClient("UrlsDetails");
            List<ShortUrlSearchResult> allResults = new List<ShortUrlSearchResult>();

            // 1. Direct lookup by vanity (PartitionKey + RowKey derived from vanity)
            if (!string.IsNullOrWhiteSpace(vanity))
            {
                var pkLookup = vanity[0].ToString().ToLowerInvariant();
                var rkLookup = vanity.ToLowerInvariant();
                try
                {
                    var entity = await table.GetEntityAsync<ShortUrlEntity>(pkLookup, rkLookup);
                    if (entity != null)
                    {
                        var e = entity.Value;
                        if (includeArchivedBool || !(e.IsArchived ?? false))
                        {
                            allResults.Add(ToResult(e));
                        }
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // Not found, return empty
                }
            }
            // 2. Partition scan or prefix scan (with optional search term)
            else if (!string.IsNullOrWhiteSpace(vanityStartsWith))
            {
                var prefix = vanityStartsWith.ToLowerInvariant();
                var partitionKey = prefix[0].ToString();
                var filter = TableClient.CreateQueryFilter<ShortUrlEntity>(e => e.PartitionKey == partitionKey && e.RowKey != "KEY");
                await foreach (var entity in table.QueryAsync<ShortUrlEntity>(filter))
                {
                    if (!includeArchivedBool && (entity.IsArchived ?? false))
                        continue;
                    // If prefix is more than one letter, filter by startsWith
                    if (prefix.Length > 1 && !entity.Vanity.StartsWith(vanityStartsWith, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        if (!(entity.Vanity.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                            || (!string.IsNullOrEmpty(entity.Title) && entity.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            || entity.Url.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }
                    allResults.Add(ToResult(entity));
                }
            }
            // 3. Full scan (not recommended for large tables, but fallback)
            else
            {
                await foreach (var entity in table.QueryAsync<ShortUrlEntity>(e => e.RowKey != "KEY"))
                {
                    if (!includeArchivedBool && (entity.IsArchived ?? false))
                        continue;
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        if (!(entity.Vanity.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                            || (!string.IsNullOrEmpty(entity.Title) && entity.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            || entity.Url.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }
                    allResults.Add(ToResult(entity));
                }
            }

            // Paging logic (1-based page)
            int totalCount = allResults.Count;
            int skip = (pageInt - 1) * pageSizeInt;
            var pageResults = allResults.Skip(skip).Take(pageSizeInt).ToList();

            var response = new McpResponseData
            {
                Data =
                {
                    ["results"] = JsonSerializer.Serialize(pageResults),
                    ["totalCount"] = totalCount.ToString(),
                    ["page"] = pageInt.ToString(),
                    ["pageSize"] = pageSizeInt.ToString(),
                },
            };
            return response;
        }

        /// <summary>
        /// Maps ShortUrlEntity to ShortUrlSearchResult, evaluating schedules for ActiveUrl.
        /// </summary>
        private static ShortUrlSearchResult ToResult(ShortUrlEntity entity)
        {
            string activeUrl = entity.Url;
            if (entity.Schedules != null && entity.Schedules.Count > 0)
            {
                activeUrl = GetActiveUrl(entity);
            }
            return new ShortUrlSearchResult
            {
                ShortUrl = entity.ShortUrl,
                Vanity = entity.Vanity,
                Title = entity.Title,
                ActiveUrl = activeUrl,
                IsArchived = entity.IsArchived ?? false,
                Timestamp = entity.Timestamp,
                ETag = entity.ETag.ToString(),
            };
        }

        /// <summary>
        /// Evaluates the schedules to determine the active URL for the given entity.
        /// </summary>
        private static string GetActiveUrl(ShortUrlEntity entity)
        {
            var now = DateTimeOffset.UtcNow;
            var link = entity.Url;
            var active = entity.Schedules.Where(s => s.End > now && s.Start < now).OrderBy(s => s.Start);
            foreach (var sched in active)
            {
                if (sched.IsActive(now))
                {
                    link = sched.AlternativeUrl;
                    break;
                }
            }
            return link;
        }

        /// <summary>
        /// Result DTO for MCP search.
        /// </summary>
        public class ShortUrlSearchResult
        {
            public string ShortUrl { get; set; }
            public string Vanity { get; set; }
            public string Title { get; set; }
            public string ActiveUrl { get; set; }
            public bool IsArchived { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public string ETag { get; set; }
        }

        /// <summary>
        /// Result DTO for MCP search response.
        /// </summary>
        public class McpResponseData
        {
            /// <summary>
            /// Key-value data for the response.
            /// </summary>
            public Dictionary<string, string> Data { get; set; } = new();
        }
    }
}
