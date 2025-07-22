using System.Collections.Concurrent;
using System.Net;

using Azure.Data.Tables;

using AzUrlShortener.Core.Service;
using AzUrlShortener.Core.Services;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzUrlShortener.Functions
{
    public class UrlRedirect
    {
        private readonly ILogger _logger;
        private TableServiceClient _tblClient;
        private static readonly ConcurrentDictionary<string, DateTimeOffset> _notFoundCache = new();
        private static readonly TimeSpan NotFoundCacheDuration = TimeSpan.FromMinutes(1);
        private static readonly long MaxCacheSizeBytes = 4 * 1024 * 1024; // 4 MB
        private static readonly Timer _cleanupTimer;
        private static readonly object _timerInitLock = new();
        private static bool _timerInitialized = false;

        static UrlRedirect()
        {
            _cleanupTimer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _timerInitialized = true;
        }

        public UrlRedirect(ILoggerFactory loggerFactory, TableServiceClient tblClient)
        {
            _logger = loggerFactory.CreateLogger<UrlRedirect>();
            _tblClient = tblClient;
            EnsureTimerStarted();
        }

        private static void EnsureTimerStarted()
        {
            if (!_timerInitialized)
            {
                lock (_timerInitLock)
                {
                    if (!_timerInitialized)
                    {
                        _cleanupTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
                        _timerInitialized = true;
                    }
                }
            }
        }

        private static void CleanupCache(object? state)
        {
            try
            {
                long cacheSize = EstimateCacheSizeBytes();
                if (cacheSize > MaxCacheSizeBytes)
                {
                    var now = DateTimeOffset.UtcNow;
                    foreach (var kvp in _notFoundCache)
                    {
                        if (kvp.Value <= now)
                        {
                            _notFoundCache.TryRemove(kvp.Key, out _);
                        }
                    }
                }
            }
            catch
            {
                // Swallow exceptions to avoid timer thread crash
            }
        }

        private static long EstimateCacheSizeBytes()
        {
            // Estimate: string (shortUrl) ~ 100 bytes, DateTimeOffset ~ 16 bytes
            // Conservative estimate: 128 bytes per entry
            return _notFoundCache.Count * 128L;
        }

        [Function("UrlRedirect")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{shortUrl?}")]
            HttpRequestData req,
            string shortUrl,
            ExecutionContext context)
        {
            if (!string.IsNullOrWhiteSpace(shortUrl))
            {
                if (_notFoundCache.TryGetValue(shortUrl, out var expiresAt))
                {
                    if (expiresAt > DateTimeOffset.UtcNow)
                    {
#if DEBUG
                        _logger.LogDebug($"Short URL '{shortUrl}' is cached as not found. Returning 404.");
#endif
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                    else
                    {
                        _notFoundCache.TryRemove(shortUrl, out _);
                    }
                }
            }

            var UrlServices = new UrlServices(_logger, new AzStorageTableService(_tblClient));

#if DEBUG
            _logger.LogDebug($"Resolving short URL '{shortUrl}'...");
#endif
            var redirectUrl = await UrlServices.Redirect(shortUrl);

            HttpResponseData result;
            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                if (!string.IsNullOrWhiteSpace(shortUrl))
                {
                    _notFoundCache[shortUrl] = DateTimeOffset.UtcNow.Add(NotFoundCacheDuration);
                }
                result = req.CreateResponse(HttpStatusCode.NotFound);
            }
            else {
                result = req.CreateResponse(HttpStatusCode.Redirect);
                result.Headers.Add("Location", redirectUrl);
            }

#if DEBUG
            _logger.LogDebug("Sending response...");
#endif
            return result;
        }
    }
}
