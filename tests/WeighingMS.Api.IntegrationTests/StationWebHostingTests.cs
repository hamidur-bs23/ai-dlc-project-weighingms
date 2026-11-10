using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>
/// The Api serving the Angular build in the Compose <c>full</c> image (E2E-FULL-STACK), and serving nothing extra when
/// there is no build, as in native development.
/// </summary>
public sealed class StationWebHostingTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string _indexMarker = "<!-- wms-test-index -->";

    private readonly string _webRoot = Directory.CreateTempSubdirectory("wms-webroot-").FullName;
    private WebApplicationFactory<Program>? _withBuild;
    private WebApplicationFactory<Program>? _withoutBuild;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "index.html"), $"<!doctype html><html><body>{_indexMarker}</body></html>", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "main.js"), "console.log('wms');", TestContext.Current.CancellationToken);
        _withBuild = fixture.CreateApi(new Dictionary<string, string?> { [WebHostDefaults.WebRootKey] = _webRoot });
        _withoutBuild = fixture.CreateApi();
    }

    public async ValueTask DisposeAsync()
    {
        if (_withBuild is not null)
        {
            await _withBuild.DisposeAsync();
        }

        if (_withoutBuild is not null)
        {
            await _withoutBuild.DisposeAsync();
        }

        Directory.Delete(_webRoot, recursive: true);
    }

    private static HttpClient Client(WebApplicationFactory<Program> api) =>
        api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    [Theory]
    [InlineData("/")]
    [InlineData("/batches")]
    [InlineData("/batches/B-24187/weigh")]
    [InlineData("/review/B-24187")]
    public async Task UiRoute_WithABuild_ServesIndexHtml(string path)
    {
        using var client = Client(_withBuild!);

        var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain(_indexMarker);
    }

    [Fact]
    public async Task BuildFile_WithABuild_IsServedAsItself()
    {
        using var client = Client(_withBuild!);

        var response = await client.GetAsync(new Uri("/main.js", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("console.log('wms');");
    }

    [Theory]
    [InlineData("/api/no-such-thing")]
    [InlineData("/api/batches/B-24187/no-such-thing")]
    public async Task UnknownApiPath_WithABuild_Stays404AndNeverHtml(string path)
    {
        using var client = Client(_withBuild!);

        var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (response.Content.Headers.ContentType?.MediaType ?? string.Empty).ShouldNotBe("text/html");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain(_indexMarker);
    }

    [Fact]
    public async Task ApiEndpoint_WithABuild_StillAnswersAsTheApi()
    {
        using var client = Client(_withBuild!);

        var response = await client.GetAsync(new Uri("/api/session", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("Auth.SignInRequired");
    }

    [Fact]
    public async Task UiRoute_WithoutABuild_IsNotServed()
    {
        using var client = Client(_withoutBuild!);

        var response = await client.GetAsync(new Uri("/batches", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
