namespace WeighingMS.Api.Common;

/// <summary>
/// Serves the Angular build from the Api's own origin in the Compose <c>full</c> image (tech-stack), so cookie auth and
/// antiforgery work without CORS. Without a <c>wwwroot</c> folder, as in native development where <c>ng serve</c> serves
/// the UI, none of this is switched on.
/// </summary>
public static class StationWebHosting
{
    /// <summary>Serves the build's files, when there is a build.</summary>
    public static WebApplication UseStationWebFiles(this WebApplication app)
    {
        if (app.Environment.WebRootPath is not null)
        {
            app.UseStaticFiles();
        }

        return app;
    }

    /// <summary>
    /// Sends the UI's own routes (for example <c>/batches/B-24187/weigh</c> opened by URL) to <c>index.html</c>. An unknown
    /// <c>/api</c> path stays a 404, so a mistyped call can never come back as an HTML page.
    /// </summary>
    public static WebApplication MapStationWeb(this WebApplication app)
    {
        if (app.Environment.WebRootPath is not null)
        {
            app.MapFallback("/api/{**rest}", () => Results.NotFound());
            app.MapFallbackToFile("index.html");
        }

        return app;
    }
}
