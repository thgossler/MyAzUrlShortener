using Azure.Data.Tables;
using AzUrlShortener.Core.Service;
using AzUrlShortener.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AzUrlShortener.Functions
{
    public class UrlRedirect
    {
        private readonly ILogger _logger;
        private TableServiceClient _tblClient;

        public UrlRedirect(ILoggerFactory loggerFactory, TableServiceClient tblClient)
        {
            _logger = loggerFactory.CreateLogger<UrlRedirect>();
            // _logger.LogDebug("UrlRedirect in constructor");
            _tblClient = tblClient;
        }

        [Function("UrlRedirect")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{shortUrl?}")]
            HttpRequestData req,
            string shortUrl,
            ExecutionContext context)
        {
            var UrlServices = new UrlServices(_logger, new AzStorageTableService(_tblClient));

#if DEBUG
            _logger.LogDebug($"Resolving short URL '{shortUrl}'...");
#endif
            var redirectUrl = await UrlServices.Redirect(shortUrl);

            HttpResponseData result;
            if (string.IsNullOrWhiteSpace(redirectUrl)) {
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
