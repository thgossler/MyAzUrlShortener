namespace AzUrlShortener.Core.Domain
{
    public class ShortenerSettings
    {
        public string DefaultRedirectUrl { get; set; }
        public string CustomDomain { get; set; }
        public string DataStorage { get; set; }
    }
}