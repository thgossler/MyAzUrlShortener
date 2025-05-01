using AzUrlShortener.Core.Domain;

namespace AzUrlShortener.Core.Service;

public interface IAzStorageTableService
{
    Task<int> GetNextTableId();
    Task<List<ShortUrlEntity>> GetAllShortUrlEntities(string ownerUpn = null);
    Task<ShortUrlEntity> SaveShortUrlEntity(ShortUrlEntity newRow2);
    Task<ShortUrlEntity> GetShortUrlEntity(ShortUrlEntity row, string ownerUpn = null);
    Task<bool> IfShortUrlEntityExist(ShortUrlEntity row, string ownerUpn = null);
    Task<ShortUrlEntity> UpdateShortUrlEntity(ShortUrlEntity urlEntity);
    Task<ShortUrlEntity> GetShortUrlEntityByVanity(string vanity, string ownerUpn = null);
    Task<bool> IfShortUrlEntityExistByVanity(string vanity, string ownerUpn = null);
    Task<ShortUrlEntity> ArchiveShortUrlEntity(ShortUrlEntity urlEntity, string ownerUpn = null);
    Task<List<ClickStatsEntity>> GetAllStatsByVanity(string vanity, string ownerUpn = null);
    Task SaveClickStatsEntity(ClickStatsEntity newStats);
}
