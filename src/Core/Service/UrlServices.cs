using Azure;
using AzUrlShortener.Core.Domain;
using AzUrlShortener.Core.Messages;
using AzUrlShortener.Core.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace AzUrlShortener.Core.Services;

public class UrlServices
{
    private readonly ILogger _logger;
    private readonly IAzStorageTableService _tableService;

    public UrlServices(ILogger logger, IAzStorageTableService storageTableService)
    {
        _logger = logger;
        _tableService = storageTableService;
    }

    public async Task<ShortUrlEntity> Archive(ShortUrlEntity input)
    {
        ShortUrlEntity result = await _tableService.ArchiveShortUrlEntity(input);
        return result;
    }

    public async Task<string> Redirect(string shortUrl)
    {
        string defaultUrl = Environment.GetEnvironmentVariable("DefaultRedirectUrl") ?? "https://azure.com";

        if (string.IsNullOrWhiteSpace(shortUrl))
        {
            return defaultUrl;
        }

        try
        {
            var tempUrl = new ShortUrlEntity(string.Empty, shortUrl);
            var newUrl = await _tableService.GetShortUrlEntity(tempUrl);

            if (newUrl == null)
            {
                _logger.LogInformation("Unknown vanity, resorting to fallback");
                return defaultUrl;
            }

            _logger.LogInformation($"Found it: {newUrl.Url}");
            newUrl.Clicks++;

            // Run both operations in parallel
            var clickTask = _tableService.SaveClickStatsEntity(new ClickStatsEntity(newUrl.Vanity));
            var updateTask = _tableService.SaveShortUrlEntity(newUrl);

            // Only wait for tasks to complete if needed for error handling
            //await Task.WhenAll(clickTask, updateTask);

            return WebUtility.UrlDecode(newUrl.ActiveUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Problem accessing storage: {ex.Message}", ex);
            return defaultUrl;
        }
    }

    public async Task<ListResponse> List(string host, string ownerUpn = null)
    {
        _logger.LogInformation($"Starting UrlList...");

        var result = new ListResponse();

        try
        {
            // Get all URLs from storage
            var allUrls = await _tableService.GetAllShortUrlEntities(ownerUpn);

            // Filter out archived URLs
            var filteredUrls = allUrls.Where(p => !(p.IsArchived ?? false));

            // If ownerUpn is provided, filter by owner
            if (!string.IsNullOrWhiteSpace(ownerUpn))
            {
                filteredUrls = filteredUrls.Where(u =>
                    string.Equals(u.OwnerUpn, ownerUpn, StringComparison.OrdinalIgnoreCase));
            }

            result.UrlList = filteredUrls.ToList();

            // Update ShortUrl property for each URL
            foreach (ShortUrlEntity url in result.UrlList)
            {
                url.ShortUrl = Utility.GetShortUrl(host, url.Vanity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error was encountered.");
            throw;
        }

        return result;
    }

    public async Task<ShortUrlEntity> Get(string host, string vanity, string ownerUpn = null, bool includeArchived = false)
    {
        _logger.LogInformation($"Starting UrlByVanity...");

        ShortUrlEntity result = null;

        try
        {
            // Get URL from storage
            var url = await _tableService.GetShortUrlEntityByVanity(vanity, ownerUpn);

            if (url == null)
            {
                return null;
            }

            // Only filter out archived URLs if includeArchived is false
            if (!includeArchived && (url.IsArchived ?? false))
            {
                return null;
            }

            // If ownerUpn is provided, filter by owner
            if (!string.IsNullOrWhiteSpace(ownerUpn) && !ownerUpn.Equals(url.OwnerUpn, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            result = url;

            // Update ShortUrl property for each URL
            url.ShortUrl = Utility.GetShortUrl(host, url.Vanity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error was encountered.");
            throw;
        }

        return result;
    }

    public async Task<ShortResponse> Create(ShortRequest input, string host)
    {
        ShortResponse result;

        try
        {
            // If the Url parameter only contains whitespaces or is empty return with BadRequest.
            if (string.IsNullOrWhiteSpace(input.Url))
            {
                throw new AzUrlShortenerException(HttpStatusCode.BadRequest, "The url parameter can not be empty.");
            }

            // Validates if input.url is a valid aboslute url, aka is a complete refrence to the resource, ex: http(s)://google.com
            if (!Regex.Match(input.Url, "^http[s]*://[0-9a-zA-Z]+.*", RegexOptions.IgnoreCase).Success)
            {
                throw new AzUrlShortenerException(HttpStatusCode.BadRequest, $"{input.Url} is not a valid absolute Url. The Url parameter must start with 'http://' or 'http://'.");
            }

            string longUrl = input.Url.Trim();
            string vanity = string.IsNullOrWhiteSpace(input.Vanity) ? "" : input.Vanity.Trim();
            string title = string.IsNullOrWhiteSpace(input.Title) ? "" : input.Title.Trim();
            string ownerUpn = string.IsNullOrWhiteSpace(input.OwnerUpn) ? "" : input.OwnerUpn.Trim();
            int clicks = input.Clicks >= 0 ? input.Clicks : 0;

            ShortUrlEntity newRow;

            if (!string.IsNullOrEmpty(vanity))
            {
                newRow = new ShortUrlEntity(longUrl, vanity, title, input.Schedules, ownerUpn);

                var existing = await _tableService.GetShortUrlEntityByVanity(vanity);
                if (existing != null)
                {
                    if (existing.IsArchived ?? false)
                    {
                        throw new AzUrlShortenerException(HttpStatusCode.Conflict, "This Short URL exists but is archived.");
                    }
                    else
                    {
                        throw new AzUrlShortenerException(HttpStatusCode.Conflict, "This Short URL already exist.");
                    }
                }
            }
            else
            {
                var generatedVanity = await Utility.GetValidEndUrl(vanity, _tableService);
                newRow = new ShortUrlEntity(longUrl, generatedVanity, title, input.Schedules);
            }

            newRow.Clicks = clicks;

            await _tableService.SaveShortUrlEntity(newRow);

            result = new ShortResponse(host, newRow.Url, newRow.RowKey, newRow.Title);

            _logger.LogInformation("Short Url created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }

        return result;
    }

    public async Task<ShortUrlEntity> Update(ShortUrlEntity input, string host)
    {
        ShortUrlEntity result;

        try
        {
            // If the Url parameter only contains whitespaces or is empty return with BadRequest.
            if (string.IsNullOrWhiteSpace(input.Url))
            {
                throw new AzUrlShortenerException(HttpStatusCode.BadRequest, "The url parameter can not be empty.");
            }

            // Validates if input.url is a valid absolute url, aka is a complete refrence to the resource, ex: http(s)://google.com
            if (!Uri.IsWellFormedUriString(input.Url, UriKind.Absolute))
            {
                throw new AzUrlShortenerException(HttpStatusCode.BadRequest, $"{input.Url} is not a valid absolute Url. The Url parameter must start with 'http://' or 'http://'.");
            }

            result = await _tableService.UpdateShortUrlEntity(input);
            result.ShortUrl = Utility.GetShortUrl(host, result.Vanity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error was encountered.");
            throw;
        }

        return result;
    }

    public async Task<ClickDateList> ClickStatsByDay(UrlClickStatsRequest input, string host, string ownerUpn = null)
    {
        var result = new ClickDateList();
        try
        {
            var rawStats = await _tableService.GetAllStatsByVanity(input.Vanity, ownerUpn);

            result.Items = rawStats.GroupBy(s => DateTime.Parse(s.ClickDatetime).Date)
                                        .Select(stat => new ClickDate
                                        {
                                            DateClicked = DateTime.Parse(stat.Key.ToString("yyyy-MM-dd")),
                                            Count = stat.Count()
                                        }).OrderBy(s => s.DateClicked).ToList<ClickDate>();

            result.Url = Utility.GetShortUrl(host, input.Vanity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error was encountered.");
            throw;
        }
        return result;
    }

    public async Task<bool> Delete(string vanity)
    {
        return await _tableService.DeleteShortUrlEntity(vanity);
    }

    public async Task<ShortUrlEntity> Clone(string sourceVanity, string newVanity)
    {
        return await _tableService.CloneShortUrlEntity(sourceVanity, newVanity);
    }

    public async Task<ShortUrlEntity> Reactivate(string vanity)
    {
        return await _tableService.ReactivateShortUrlEntity(vanity);
    }
}

