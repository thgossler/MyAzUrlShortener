using Azure.Data.Tables;
using Cloud5mins.ShortenerTools.Core.Service;
using Cloud5mins.ShortenerTools.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Cloud5mins.ShortenerTools.Functions
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
            string? shortUrl,
            ExecutionContext context)
        {
            _logger.LogDebug("Function reached");
            UrlServices UrlServices = new UrlServices(_logger, new AzStorageTableService(_tblClient));
            _logger.LogDebug("Services created");

            _logger.LogDebug($"Redirecting {shortUrl}...");
            string redirectUrl = await UrlServices.Redirect(shortUrl);
            _logger.LogDebug("got the redirect url");

            _logger.LogDebug($"Creating response...");
            var res = req.CreateResponse(HttpStatusCode.Redirect);
            res.Headers.Add("Location", redirectUrl);
            _logger.LogDebug("response created");
            return res;
        }
    }
}
