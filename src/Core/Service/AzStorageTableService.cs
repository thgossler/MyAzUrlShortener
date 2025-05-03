using Azure;
using Azure.Data.Tables;
using AzUrlShortener.Core.Domain;
using System.Text.Json;

namespace AzUrlShortener.Core.Service;

public class AzStorageTableService(TableServiceClient client) : IAzStorageTableService
{
    private TableClient GetUrlsTable()
    {
        client.CreateTableIfNotExists("UrlsDetails");
        TableClient table = client.GetTableClient("UrlsDetails");
        return table;
    }

    private TableClient GetStatsTable()
    {
        client.CreateTableIfNotExists("ClickStats");
        TableClient table = client.GetTableClient("ClickStats");
        return table;
    }

    public async Task<int> GetNextTableId()
    {
        //Get current ID
        TableClient tblUrls = GetUrlsTable();
        NextId entity;

        var check = await tblUrls.GetEntityIfExistsAsync<NextId>("1", "KEY");
        if (check.HasValue)
        {
            var result = await tblUrls.GetEntityAsync<NextId>("1", "KEY");
            entity = result.Value as NextId;
        }
        else
        {
            entity = new NextId
            {
                PartitionKey = "1",
                RowKey = "KEY",
                Id = 1024
            };
        }

        entity.Id++;

        //Update
        await tblUrls.UpsertEntityAsync(entity);

        return entity.Id;
    }

    public async Task<List<ShortUrlEntity>> GetAllShortUrlEntities(string ownerUpn = null)
    {
        TableClient tblUrls = GetUrlsTable();
        var lstShortUrl = new List<ShortUrlEntity>();

        // Retreiving all entities that are NOT the NextId entity 
        // (it's the only one in the partion "KEY")
        AsyncPageable<ShortUrlEntity> queryResult;

        if (string.IsNullOrEmpty(ownerUpn))
        {
            queryResult = tblUrls.QueryAsync<ShortUrlEntity>(e => e.RowKey != "KEY");
        }
        else
        {
            queryResult = tblUrls.QueryAsync<ShortUrlEntity>(e => e.RowKey != "KEY" && 
                e.OwnerUpn == ownerUpn.Trim().ToLowerInvariant());
        }

        await foreach (var emp in queryResult.AsPages())
        {
            foreach (var item in emp.Values)
            {
                lstShortUrl.Add(item);
            }
        }

        return lstShortUrl;
    }

    public async Task<ShortUrlEntity> SaveShortUrlEntity(ShortUrlEntity newShortUrl)
    {
        // serializing the collection easier on json shares
        //newShortUrl.SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(newShortUrl.Schedules);

        TableClient tblUrls = GetUrlsTable();
        var response = await tblUrls.UpsertEntityAsync<ShortUrlEntity>(newShortUrl);

        var temp = response.Content;
        return newShortUrl;
    }

    public async Task<ShortUrlEntity> UpdateShortUrlEntity(ShortUrlEntity urlEntity)
    {
        ShortUrlEntity originalUrl = await GetShortUrlEntity(urlEntity);
        originalUrl.Url = urlEntity.Url;
        originalUrl.Title = urlEntity.Title;
        originalUrl.SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(urlEntity.Schedules);
        originalUrl.OwnerUpn = urlEntity.OwnerUpn;

        return await SaveShortUrlEntity(originalUrl);
    }

    /// <summary>
    /// Returns the ShortUrlEntity of the <paramref name="vanity"/>
    /// </summary>
    /// <param name="vanity">The vanity URL to search for</param>
    /// <param name="ownerUpn">Optional owner UPN to filter by</param>
    /// <returns>ShortUrlEntity</returns>
    public async Task<ShortUrlEntity> GetShortUrlEntityByVanity(string vanity, string ownerUpn = null)
    {
        var tblUrls = GetUrlsTable();
        ShortUrlEntity shortUrlEntity = null;

        AsyncPageable<ShortUrlEntity> result;

        vanity = vanity.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(ownerUpn))
        {
            result = tblUrls.QueryAsync<ShortUrlEntity>(e => e.RowKey == vanity);
        }
        else
        {
            result = tblUrls.QueryAsync<ShortUrlEntity>(e => e.RowKey == vanity && 
                e.OwnerUpn == ownerUpn.Trim().ToLowerInvariant());
        }

        await foreach (var entity in result)
        {
            shortUrlEntity = entity;
            break;
        }
        return shortUrlEntity;
    }

    public async Task<ShortUrlEntity> GetShortUrlEntity(ShortUrlEntity row, string ownerUpn = null)
    {
        TableClient tblUrls = GetUrlsTable();
        var response = await tblUrls.GetEntityAsync<ShortUrlEntity>(row.PartitionKey, row.RowKey);
        ShortUrlEntity eShortUrl = response.Value as ShortUrlEntity;

        // Filter by owner if specified
        if (!string.IsNullOrEmpty(ownerUpn) && eShortUrl?.OwnerUpn != ownerUpn.Trim().ToLowerInvariant())
        {
            return null;
        }

        return eShortUrl;
    }

    public async Task<bool> IfShortUrlEntityExistByVanity(string vanity, string ownerUpn = null)
    {
        ShortUrlEntity shortUrlEntity = await GetShortUrlEntityByVanity(vanity, ownerUpn);
        return (shortUrlEntity != null);
    }

    public async Task<bool> IfShortUrlEntityExist(ShortUrlEntity row, string ownerUpn = null)
    {
        TableClient tblUrls = GetUrlsTable();
        var result = await tblUrls.GetEntityIfExistsAsync<ShortUrlEntity>(row.PartitionKey, row.RowKey);

        if (!result.HasValue)
        {
            return false;
        }

        // If ownerUpn is specified, check if it matches
        if (!string.IsNullOrEmpty(ownerUpn))
        {
            var entity = result.Value as ShortUrlEntity;
            return entity.OwnerUpn == ownerUpn.Trim().ToLowerInvariant();
        }

        return true;
    }

    public async Task<ShortUrlEntity> ArchiveShortUrlEntity(ShortUrlEntity urlEntity, string ownerUpn = null)
    {
        ShortUrlEntity originalUrl = await GetShortUrlEntity(urlEntity, ownerUpn);

        // Return null if entity not found or owner doesn't match
        if (originalUrl == null)
        {
            return null;
        }

        originalUrl.IsArchived = true;

        return await SaveShortUrlEntity(originalUrl);
    }

    public async Task<List<ClickStatsEntity>> GetAllStatsByVanity(string vanity, string ownerUpn = null)
    {
        var tblStats = GetStatsTable();
        var lstUrlStats = new List<ClickStatsEntity>();

        // If owner is specified, first verify the URL belongs to that owner
        if (!string.IsNullOrEmpty(ownerUpn) && !string.IsNullOrEmpty(vanity))
        {
            var urlEntity = await GetShortUrlEntityByVanity(vanity, ownerUpn);
            if (urlEntity == null)
            {
                // URL doesn't exist or doesn't belong to the specified owner
                return lstUrlStats;
            }
        }

        AsyncPageable<ClickStatsEntity> queryResult;

        if (string.IsNullOrEmpty(vanity))
        {
            // If no vanity is specified but owner is specified, we need to get all URLs for the owner first
            if (!string.IsNullOrEmpty(ownerUpn))
            {
                var ownerUrls = await GetAllShortUrlEntities(ownerUpn);
                var allOwnerStats = new List<ClickStatsEntity>();

                foreach (var url in ownerUrls)
                {
                    var urlStats = await GetAllStatsByVanity(url.RowKey);
                    allOwnerStats.AddRange(urlStats);
                }

                return allOwnerStats;
            }
            else
            {
                queryResult = tblStats.QueryAsync<ClickStatsEntity>();
            }
        }
        else
        {
            queryResult = tblStats.QueryAsync<ClickStatsEntity>(e => e.PartitionKey == vanity.ToLowerInvariant());
        }

        await foreach (var emp in queryResult.AsPages())
        {
            foreach (var item in emp.Values)
            {
                lstUrlStats.Add(item);
            }
        }

        return lstUrlStats;
    }

    public async Task SaveClickStatsEntity(ClickStatsEntity newStats)
    {
        var result = await GetStatsTable().UpsertEntityAsync(newStats);
    }
}
