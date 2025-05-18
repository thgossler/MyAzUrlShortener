using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AzUrlShortener.Core.Domain
{
    public class ShortUrlEntity : ITableEntity
    {
        public string Url { get; set; }

        public string ActiveUrl
        {
            get
            {
                return GetActiveUrl();
            }
        }

        private string _ownerUpn;

        /// <summary>
        /// The User Principal Name (email) of the user who created or owns this short URL.
        /// </summary>
        public string OwnerUpn { get => _ownerUpn; set => _ownerUpn = value?.Trim().ToLowerInvariant(); }

        /// <summary>
        /// The title of the short URL. This is used for display purposes only and does not affect the functionality of the short URL.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The end URL (or vanity) is the suffix of the short URL that will be used to access the long URL.
        /// RowKey is the end URL in lower case, Vanity is the case-sensitive end URL as provided by the user.
        /// </summary>
        public string Vanity { get; set; }

        /// <summary>
        /// The short URL is the full short URL that will be used to access the long URL. It will 
        /// be updated from extern upon access.
        /// </summary>
        public string ShortUrl { get; set; }

        /// <summary>
        /// The number of clicks on the short URL.
        /// </summary>
        public int Clicks { get; set; }

        /// <summary>
        /// Indicates whether the short URL is archived or not.
        /// </summary>
        public bool? IsArchived { get; set; }

        /// <summary>
        /// The raw JSON string representation of the schedules for the short URL. This is used for serialization/deserialization.
        /// </summary>
        public string SchedulesPropertyRaw { get; set; }

        private List<Schedule> _schedules;

        /// <summary>
        /// The list of schedules for the short URL. Each schedule contains a start and end date/time, and an alternative URL.
        /// </summary>
        [IgnoreDataMember]
        public List<Schedule> Schedules
        {
            get
            {
                if (_schedules == null)
                {
                    if (string.IsNullOrEmpty(SchedulesPropertyRaw))
                    {
                        _schedules = new List<Schedule>();
                    }
                    else
                    {
                        var deserializedSchedules = JsonSerializer.Deserialize<Schedule[]>(SchedulesPropertyRaw);
                        _schedules = deserializedSchedules?.ToList() ?? new List<Schedule>();
                    }
                }
                return _schedules;
            }
            set
            {
                _schedules = value;
            }
        }

        /// <summary>
        /// The PartitionKey is the first character of the end URL in lower case, which is used to partition the table storage.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// The RowKey is the end URL in lower case, which is used to uniquely identify the short URL in the table storage.
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// The timestamp of the last update to the short URL entity. This is set by Azure Table Storage.
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// The ETag is used for optimistic concurrency control. It is set by Azure Table Storage.
        /// </summary>
        public ETag ETag { get; set; }

        /// <summary>
        /// Default constructor for the ShortUrlEntity class. Needed for serialization/deserialization,
        /// should not be used directly because it doesn't initialize the properties.
        /// </summary>
        public ShortUrlEntity() 
        { 
        }

        public ShortUrlEntity(string longUrl, string endUrl)
        {
            Initialize(longUrl, endUrl, string.Empty, null);
        }

        public ShortUrlEntity(string longUrl, string endUrl, Schedule[] schedules)
        {
            Initialize(longUrl, endUrl, string.Empty, schedules);
        }

        public ShortUrlEntity(string longUrl, string endUrl, string title, Schedule[] schedules, string ownerUpn = null)
        {
            Initialize(longUrl, endUrl, title, schedules, ownerUpn);
        }

        private void Initialize(string longUrl, string endUrl, string title, Schedule[] schedules, string ownerUpn = null)
        {
            endUrl = endUrl.Trim();
            if (string.IsNullOrEmpty(endUrl))
            {
                throw new ArgumentException("End URL cannot be null or empty.", nameof(endUrl));
            }

            PartitionKey = endUrl.First().ToString().ToLowerInvariant();
            RowKey = endUrl.ToLowerInvariant();
            Vanity = endUrl;
            Url = longUrl;
            Title = title;
            Clicks = 0;
            IsArchived = false;
            OwnerUpn = ownerUpn;

            if (schedules?.Length > 0)
            {
                Schedules = schedules.ToList<Schedule>();
                SchedulesPropertyRaw = JsonSerializer.Serialize<List<Schedule>>(Schedules);
            }
        }

        public static ShortUrlEntity GetNewEntity(string longUrl, string endUrl, string title, Schedule[] schedules, string ownerUpn = null)
        {
            var e = new ShortUrlEntity();
            e.Initialize(longUrl, endUrl, title, schedules, ownerUpn);
            return e;
        }

        private string GetActiveUrl()
        {
            if (Schedules != null && Schedules.Count > 0)
                return GetActiveUrl(DateTimeOffset.UtcNow);
            return Url;
        }

        private string GetActiveUrl(DateTimeOffset pointInTime)
        {
            var link = Url;
            var active = Schedules.Where(s =>
                s.End > pointInTime && //hasn't ended
                s.Start < pointInTime //already started
                ).OrderBy(s => s.Start); //order by start to process first link

            foreach (var sched in active.ToArray())
            {
                if (sched.IsActive(pointInTime))
                {
                    link = sched.AlternativeUrl;
                    break;
                }
            }
            return link;
        }

        public bool Validate()
        {
            return Validate(Url, Vanity, Title, Schedules.ToArray());
        }

        public static bool Validate(string url, string vanity, string title, Schedule[] schedules)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Regex.Match(url, "^http[s]*://[0-9a-zA-Z]+.*", RegexOptions.IgnoreCase).Success) return false;

            if (string.IsNullOrWhiteSpace(vanity)) return false;
            if (!Regex.Match(vanity, "^[0-9a-zA-Z-_]+$", RegexOptions.IgnoreCase).Success) return false;

            if (string.IsNullOrWhiteSpace(title)) return false;

            return true;
        }
    }
}
