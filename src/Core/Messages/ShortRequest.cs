using AzUrlShortener.Core.Domain;

namespace AzUrlShortener.Core.Messages
{
    public class ShortRequest
    {
        public string Vanity { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Schedule[] Schedules { get; set; }
        public int Clicks { get; set; }
        public string OwnerUpn { get; set; } = string.Empty;
    }
}