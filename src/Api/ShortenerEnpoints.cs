using Azure.Data.Tables;
using AzUrlShortener.Core.Domain;
using AzUrlShortener.Core.Messages;
using AzUrlShortener.Core.Service;
using AzUrlShortener.Core.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;

public static class ShortenerEnpoints
{
    public static void MapShortenerEnpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("api")
                .WithOpenApi();

        // GETS

        endpoints.MapGet("/", GetWelcomeMessage)
            .WithDescription("Welcome to AzUrlShortener API");

        endpoints.MapGet("/UrlList", UrlList)
            .WithDescription("List all Urls")
            .WithDisplayName("Url List");

        endpoints.MapGet("/Url/{vanity}", UrlByVanity)
            .WithDescription("Get Url by Vanity")
            .WithDisplayName("Url By Vanity");


        // POSTS

        endpoints.MapPost("/UrlCreate", UrlCreate)
            .WithDescription("Create a new Short URL")
            .WithDisplayName("Url Create");

        endpoints.MapPost("/UrlUpdate", UrlUpdate)
            .WithDescription("Update a Url")
            .WithDisplayName("Url Update");

        endpoints.MapPost("/UrlArchive", UrlArchive)
            .WithDescription("Archive a Url")
            .WithDisplayName("Url Archive");

        endpoints.MapPost("/UrlClickStatsByDay", UrlClickStatsByDay)
            .WithDescription("Provide Click Statistics by Day")
            .WithDisplayName("Url Click Statistics By Day");

        endpoints.MapPost("/UrlDelete", UrlDelete)
            .WithDescription("Delete a Url")
            .WithDisplayName("Url Delete");

        endpoints.MapPost("/UrlClone", UrlClone)
            .WithDescription("Clone a Url")
            .WithDisplayName("Url Clone");

        endpoints.MapPost("/UrlReactivate", UrlReactivate)
            .WithDescription("Reactivate a Url")
            .WithDisplayName("Url Reactivate");
    }

    private static string GetWelcomeMessage()
    {
        return "Welcome to AzUrlShortener API";
    }

    private static async Task<Results<
                                Created<ShortResponse>,
                                BadRequest<DetailedBadRequest>,
                                NotFound<DetailedBadRequest>,
                                Conflict<DetailedBadRequest>,
                                InternalServerError<DetailedBadRequest>
                                >> UrlCreate(ShortRequest request,
                                                TableServiceClient tblClient,
                                                HttpContext context,
                                                ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var host = GetFullHostName(context);
            ShortResponse result = await urlServices.Create(request, host);
            return TypedResults.Created($"/api/UrlCreate/{result.ShortUrl}", result);
        }
        catch (AzUrlShortenerException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return TypedResults.BadRequest<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
                case HttpStatusCode.NotFound:
                    return TypedResults.NotFound<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
                case HttpStatusCode.Conflict:
                    return TypedResults.Conflict<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
                default:
                    return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error was encountered.");
            return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
        }
    }

    private static async Task<Results<
                                    Ok,
                                    InternalServerError<DetailedBadRequest>>>
                                    UrlArchive(ShortUrlEntity shortUrl,
                                                TableServiceClient tblClient,
                                                ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var result = await urlServices.Archive(shortUrl);
            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
        }
    }

    private static async Task<Results<
                                    Ok<ShortUrlEntity>,
                                    InternalServerError<DetailedBadRequest>>>
                                    UrlUpdate(ShortUrlEntity shortUrl,
                                                TableServiceClient tblClient,
                                                HttpContext context,
                                                ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var host = GetFullHostName(context);
            var result = await urlServices.Update(shortUrl, host);
            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
        }
    }



    private static async Task<Results<
                                    Ok<ClickDateList>,
                                    InternalServerError<DetailedBadRequest>>>
                                    UrlClickStatsByDay(UrlClickStatsRequest statsRequest,
                                                TableServiceClient tblClient,
                                                HttpContext context,
                                                ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var host = GetFullHostName(context);

            // Get the ownerUpn query parameter if it exists
            string ownerUpn = context.Request.Query["ownerUpn"];

            var result = await urlServices.ClickStatsByDay(statsRequest, host, ownerUpn);
            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
        }
    }


    private static async Task<Results<
                            Ok<ListResponse>,
                            InternalServerError<DetailedBadRequest>>>
                            UrlList(TableServiceClient tblClient,
                                    HttpContext context,
                                    ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var host = GetFullHostName(context);

            // Get the ownerUpn query parameter if it exists
            string ownerUpn = context.Request.Query["ownerUpn"];

            // Pass the ownerUpn to the List method
            ListResponse urls = await urlServices.List(host, ownerUpn);
            return TypedResults.Ok(urls);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error was encountered.");
            return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
        }
    }


    private static async Task<Results<
                            Ok<ShortUrlEntity>,
                            InternalServerError<DetailedBadRequest>>>
                            UrlByVanity(TableServiceClient tblClient,
                                    HttpContext context,
                                    string vanity,
                                    ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var host = GetFullHostName(context);

            if (string.IsNullOrEmpty(vanity))
            {
                return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = "Vanity parameter is required." });
            }

            // Get the ownerUpn query parameter if it exists
            string ownerUpn = context.Request.Query["ownerUpn"];
            // Get the includeArchived query parameter if it exists
            bool includeArchived = false;
            if (context.Request.Query.ContainsKey("includeArchived"))
            {
                bool.TryParse(context.Request.Query["includeArchived"], out includeArchived);
            }

            // Pass the ownerUpn and includeArchived to the Get method
            ShortUrlEntity url = await urlServices.Get(host, vanity, ownerUpn, includeArchived);
            return TypedResults.Ok(url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error was encountered.");
            return TypedResults.InternalServerError<DetailedBadRequest>(new DetailedBadRequest { Message = ex.Message });
        }
    }

    private static async Task<Results<Ok, NotFound<DetailedBadRequest>, InternalServerError<DetailedBadRequest>>>
        UrlDelete([FromBody] string vanity, TableServiceClient tblClient, ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var result = await urlServices.Delete(vanity);
            if (result)
                return TypedResults.Ok();
            return TypedResults.NotFound(new DetailedBadRequest { Message = "URL not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return TypedResults.InternalServerError(new DetailedBadRequest { Message = ex.Message });
        }
    }

    private static async Task<Results<Ok<ShortUrlEntity>, NotFound<DetailedBadRequest>, InternalServerError<DetailedBadRequest>>>
        UrlClone([FromBody] CloneRequest req, TableServiceClient tblClient, ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var result = await urlServices.Clone(req.SourceVanity, req.NewVanity);
            if (result != null)
                return TypedResults.Ok(result);
            return TypedResults.NotFound(new DetailedBadRequest { Message = "Source URL not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return TypedResults.InternalServerError(new DetailedBadRequest { Message = ex.Message });
        }
    }

    private static async Task<Results<Ok<ShortUrlEntity>, NotFound<DetailedBadRequest>, InternalServerError<DetailedBadRequest>>>
        UrlReactivate([FromBody] string vanity, TableServiceClient tblClient, ILogger logger)
    {
        try
        {
            var urlServices = new UrlServices(logger, new AzStorageTableService(tblClient));
            var result = await urlServices.Reactivate(vanity);
            if (result != null)
                return TypedResults.Ok(result);
            return TypedResults.NotFound(new DetailedBadRequest { Message = "URL not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return TypedResults.InternalServerError(new DetailedBadRequest { Message = ex.Message });
        }
    }

    private static string GetFullHostName(HttpContext context)
    {
        // Need to get the external endpoint from environment
        var funcUrl = Environment.GetEnvironmentVariable("services__functions__https__0");
        if (string.IsNullOrEmpty(funcUrl))
        {
            funcUrl = Environment.GetEnvironmentVariable("services__functions__http__0");
        }

        // Get the custom domain endpoint if configured
        var customDomain = Environment.GetEnvironmentVariable("CustomDomain");

        var host = !string.IsNullOrWhiteSpace(customDomain) && !funcUrl.Contains("localhost") ? customDomain : funcUrl;
        if (string.IsNullOrWhiteSpace(host))
        {
            // Falling-back to same host/port
            host = $"{context.Request.Scheme}://{context.Request.Host.Value}";
        }
        if (host != null && !host.StartsWith("http"))
        {
            // Use same protocol is not specified
            host = $"{context.Request.Scheme}://{host}";
        }

        return host ?? string.Empty;
    }
}

public class CloneRequest
{
    public string SourceVanity { get; set; }
    public string NewVanity { get; set; }
}
