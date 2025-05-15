using Cronos;

namespace AzUrlShortener.Core.Domain
{
    public class Schedule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset Start { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset End { get; set; } = DateTimeOffset.MaxValue;

        public string Cron { get; set; } = "* * * * *";
        public int DurationMinutes { get; set; } = 0;

        public string AlternativeUrl { get; set; } = string.Empty;

        public string GetDisplayableUrl(int max)
        {
            var length = AlternativeUrl.ToString().Length;
            if (length >= max)
            {
                return string.Concat(AlternativeUrl.Substring(0, max - 1), "...");
            }
            return AlternativeUrl;
        }

        public bool IsActive(DateTimeOffset pointInTime)
        {
            // Check if pointInTime is between Start and End
            if (pointInTime < Start || pointInTime > End)
            {
                return false;
            }

            // Check if cron expression is unlimited (empty or just asterisks)
            if (string.IsNullOrEmpty(Cron) || Cron.Trim() == "* * * * *")
            {
                return true;
            }

            if (DurationMinutes <= 0)
            {
                return false;
            }

            // For specific cron expressions, check if pointInTime is within a scheduled time window
            var bufferStart = pointInTime.AddMinutes(-DurationMinutes).UtcDateTime;
            var bufferEnd = pointInTime.AddMinutes(DurationMinutes).UtcDateTime;

            CronExpression expression = CronExpression.Parse(Cron);
            var potentialOccurrences = expression.GetOccurrences(bufferStart, bufferEnd);
            var matchingOccurrences = potentialOccurrences.Where(o => o <= pointInTime && pointInTime < o.AddMinutes(DurationMinutes));
            var isMatch = matchingOccurrences.Count() > 0;
            
            return isMatch;
        }

    }
}
