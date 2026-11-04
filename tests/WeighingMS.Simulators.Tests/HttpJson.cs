using System.Net.Http.Json;
using System.Text.Json;

namespace WeighingMS.Simulators.Tests;

/// <summary>Small helpers for reading JSON responses in contract tests.</summary>
internal static class HttpJson
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return doc.RootElement.Clone();
    }

    public static async Task<string?> ProblemTypeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("type").GetString();

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body, TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> GetUrlAsync(this HttpClient client, string url) =>
        client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);
}
