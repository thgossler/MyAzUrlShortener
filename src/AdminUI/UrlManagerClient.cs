using AzUrlShortener.Core.Domain;
using AzUrlShortener.Core.Messages;
using AzUrlShortener.AdminUI.Services;

namespace AzUrlShortener.AdminUI;

public class UrlManagerClient
{
    private readonly HttpClient _httpClient;
    private readonly UserService _userService;

    public UrlManagerClient(HttpClient httpClient, UserService userService)
    {
        _httpClient = httpClient;
        _userService = userService;
    }

    public async Task<IQueryable<ShortUrlEntity>> GetUrls()
    {
        IQueryable<ShortUrlEntity> urlList = new List<ShortUrlEntity>().AsQueryable();
        try
        {
            // Check if user is admin
            var isAdmin = await _userService.IsAdminAsync();
            string requestUrl = "/api/UrlList";

            // If not admin and regular users can't view all records, filter by owner
            if (!isAdmin && !_userService.CanRegularUsersViewAllRecords())
            {
                var currentUpn = await _userService.GetUserPrincipalNameAsync();
                requestUrl = $"{requestUrl}?ownerUpn={Uri.EscapeDataString(currentUpn)}";
            }

            using var response = await _httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                var urls = await response.Content.ReadFromJsonAsync<ListResponse>();
                urlList = urls!.UrlList.AsQueryable<ShortUrlEntity>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return urlList;
    }

    public async Task<ShortUrlEntity> GetUrlByVanity(string vanity)
    {
        ShortUrlEntity url = null;
        try
        {
            // Check if user is admin
            var isAdmin = await _userService.IsAdminAsync();
            string requestUrl = $"/api/Url/{vanity}?includeArchived=true";

            // If not admin and regular users can't view all records, filter by owner
            if (!isAdmin && !_userService.CanRegularUsersViewAllRecords())
            {
                var currentUpn = await _userService.GetUserPrincipalNameAsync();
                requestUrl += $"&ownerUpn={Uri.EscapeDataString(currentUpn)}";
            }

            using var response = await _httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                url = await response.Content.ReadFromJsonAsync<ShortUrlEntity>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return url;
    }

    public async Task<(bool, string)> UrlCreate(ShortRequest url)
    {
        (bool, string) result = (false, "Failed");
        try
        {
            // Set the owner to current user
            url.OwnerUpn = await _userService.GetUserPrincipalNameAsync();

            using var response = await _httpClient.PostAsJsonAsync<ShortRequest>("/api/UrlCreate", url);
            if (response.IsSuccessStatusCode)
            {
                result = (true, "Success");
            }
            else
            {
                var errorDetails = await response.Content.ReadFromJsonAsync<DetailedBadRequest>();
                result = (false, errorDetails!.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            result = (false, ex.Message);
        }
        return result;
    }

    public async Task<bool> UrlArchive(ShortUrlEntity shortUrl)
    {
        try
        {
            // Check if user has rights to archive
            var isAdmin = await _userService.IsAdminAsync();
            var currentUpn = await _userService.GetUserPrincipalNameAsync();

            if (!isAdmin)
            {
                // First check: user must be the owner
                if (!string.Equals(shortUrl.OwnerUpn, currentUpn, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Second check: users must be allowed to archive their own records
                if (!_userService.CanRegularUsersArchiveRecords())
                {
                    return false;
                }
            }

            using var response = await _httpClient.PostAsJsonAsync("/api/UrlArchive", shortUrl);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return false;
    }

    public async Task<ShortUrlEntity> UrlUpdate(ShortUrlEntity shortUrl)
    {
        try
        {
            // Check if user has rights to update
            var isAdmin = await _userService.IsAdminAsync();
            var currentUpn = await _userService.GetUserPrincipalNameAsync();

            if (!isAdmin && !string.Equals(shortUrl.OwnerUpn, currentUpn, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            using var response = await _httpClient.PostAsJsonAsync("/api/UrlUpdate", shortUrl);
            if (response.IsSuccessStatusCode)
            {
                var updatedUrl = await response.Content.ReadFromJsonAsync<ShortUrlEntity>();
                return updatedUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return null;
    }

    public async Task<ClickDateList> UrlClickStatsByDay(UrlClickStatsRequest statsRequest)
    {
        try
        {
            var isAdmin = await _userService.IsAdminAsync();

            string requestUrl = "/api/UrlClickStatsByDay";

            // If not admin, add the ownerUpn as a query parameter to filter at the table storage level
            if (!isAdmin)
            {
                var currentUpn = await _userService.GetUserPrincipalNameAsync();
                requestUrl = $"{requestUrl}?ownerUpn={Uri.EscapeDataString(currentUpn)}";
            }

            using var response = await _httpClient.PostAsJsonAsync(requestUrl, statsRequest);
            if (response.IsSuccessStatusCode)
            {
                var clickStats = await response.Content.ReadFromJsonAsync<ClickDateList>();
                return clickStats;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return null;
    }

    public async Task<bool> UrlDelete(ShortUrlEntity shortUrl)
    {
        try
        {
            var isAdmin = await _userService.IsAdminAsync();
            var currentUpn = await _userService.GetUserPrincipalNameAsync();

            if (!isAdmin)
            {
                // First check: user must be the owner
                if (!string.Equals(shortUrl.OwnerUpn, currentUpn, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Second check: users must be allowed to delete their own records
                if (!_userService.CanRegularUsersDeleteRecords())
                {
                    return false;
                }
            }

            using var response = await _httpClient.PostAsJsonAsync("/api/UrlDelete", shortUrl.Vanity);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return false;
    }

    public async Task<ShortUrlEntity> UrlClone(ShortUrlEntity sourceShortUrl, string newVanity)
    {
        try
        {
            var isAdmin = await _userService.IsAdminAsync();
            var currentUpn = await _userService.GetUserPrincipalNameAsync();

            if (!isAdmin)
            {
                // First check: user must be the owner
                if (!string.Equals(sourceShortUrl.OwnerUpn, currentUpn, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // Second check: users must be allowed to delete their own records
                if (!_userService.CanRegularUsersDeleteRecords())
                {
                    return null;
                }
            }

            var req = new { SourceVanity = sourceShortUrl.Vanity, NewVanity = newVanity };
            using var response = await _httpClient.PostAsJsonAsync("/api/UrlClone", req);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ShortUrlEntity>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }

    public async Task<ShortUrlEntity> UrlReactivate(string vanity, string ownerUpn)
    {
        try
        {
            var currentUserUpn = await _userService.GetUserPrincipalNameAsync();
            var isAdmin = await _userService.IsAdminAsync();
            var isOwner = string.Equals(ownerUpn, currentUserUpn, StringComparison.OrdinalIgnoreCase);
            var canArchive = isAdmin || (isOwner && _userService.CanRegularUsersArchiveRecords());
            if (!canArchive) return null;

            using var response = await _httpClient.PostAsJsonAsync("/api/UrlReactivate", vanity);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ShortUrlEntity>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }
}
