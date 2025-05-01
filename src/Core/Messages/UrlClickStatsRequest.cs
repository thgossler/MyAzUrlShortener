namespace AzUrlShortener.Core.Messages
{
    public class UrlClickStatsRequest
    {
        public string Vanity { get; set; } = string.Empty;

        public UrlClickStatsRequest(string vanity)
        {
            Vanity = vanity;
        }
    }
}