using System.Text.Json;

namespace WeighingMS.Api.IntegrationTests;

internal static class Json
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return doc.RootElement.Clone();
    }

    public static async Task<string?> ErrorCodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("type").GetString();
}
