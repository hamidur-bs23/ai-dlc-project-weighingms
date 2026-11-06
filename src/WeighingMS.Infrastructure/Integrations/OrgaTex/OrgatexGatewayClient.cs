using System.Net.Http.Json;
using System.Text.Json;
using ErrorOr;
using Polly.CircuitBreaker;
using Polly.Timeout;
using WeighingMS.Application.Batches;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Infrastructure.Integrations.OrgaTex;

/// <summary>
/// <see cref="IOrgatexGateway"/> over the Dummy OrgaTEX training contract (SIM-CONTRACTS): translates its shapes into
/// WeighingMS types and every transport failure into <see cref="OrgatexErrors.Unavailable"/>. Not a claim about the
/// real OrgaTEX interface.
/// </summary>
public sealed class OrgatexGatewayClient(HttpClient http) : IOrgatexGateway
{
    /// <inheritdoc />
    public async Task<ErrorOr<IReadOnlyList<OrgatexBatch>>> GetBatchesAsync(CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(ct => http.GetAsync(new Uri("api/batches", UriKind.Relative), ct), OrgatexErrors.Unavailable, null, cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var batches = await message.Content.ReadFromJsonAsync<List<SimBatch>>(IntegrationCall.Json, cancellationToken) ?? [];
        return batches.Select(ToBatch).ToList();
    }

    /// <inheritdoc />
    public async Task<ErrorOr<OrgatexBatch>> GetBatchAsync(string batchId, CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri($"api/batches/{Uri.EscapeDataString(batchId)}", UriKind.Relative), ct),
            OrgatexErrors.Unavailable, BatchErrors.NotFound(batchId), cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var batch = await message.Content.ReadFromJsonAsync<SimBatch>(IntegrationCall.Json, cancellationToken);
        return batch is null ? OrgatexErrors.Unavailable : ToBatch(batch);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<OrgatexRecipe>> GetRecipeAsync(string batchId, CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri($"api/batches/{Uri.EscapeDataString(batchId)}/recipe", UriKind.Relative), ct),
            OrgatexErrors.Unavailable, BatchErrors.NotFound(batchId), cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var recipe = await message.Content.ReadFromJsonAsync<SimRecipe>(IntegrationCall.Json, cancellationToken);
        if (recipe is null)
        {
            return OrgatexErrors.Unavailable;
        }

        var lines = recipe.Components.Select(c => new RecipeLine(
            c.Sequence, c.ChemicalId, c.Code, c.Name, c.DisplayColour, c.TargetQuantity, c.Unit, c.Tolerance, c.SuggestedContainer)).ToList();
        return new OrgatexRecipe(recipe.BatchId, recipe.Shade, recipe.TrainingOnly, lines);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<DateTimeOffset>> PostConsumptionAsync(string batchId, Guid idempotencyKey, IReadOnlyList<ConsumptionLine> lines, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lines);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"api/batches/{Uri.EscapeDataString(batchId)}/consumption", UriKind.Relative))
        {
            Content = JsonContent.Create(new SimConsumption([.. lines.Select(l => new SimConsumptionLine(l.ChemicalId, l.ActualQuantity, l.Unit, l.Lot))]), options: IntegrationCall.Json),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return OrgatexErrors.Unavailable; // transient: retried with back-off (WRITEBACK-TRANSIENT)
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var receipt = await response.Content.ReadFromJsonAsync<SimReceipt>(IntegrationCall.Json, cancellationToken);
                return receipt?.ReceivedAt ?? DateTimeOffset.UtcNow;
            }

            if ((int)response.StatusCode >= 500)
            {
                return OrgatexErrors.Unavailable;
            }

            // A 4xx: retrying the same request can't succeed, so it is a rejection (sync failed at once).
            var problem = await ReadProblemCodeAsync(response, cancellationToken);
            return OrgatexErrors.ConsumptionRejected(problem, (int)response.StatusCode);
        }
    }

    private static async Task<string> ReadProblemCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<SimProblem>(IntegrationCall.Json, cancellationToken);
            return string.IsNullOrWhiteSpace(problem?.Type) ? $"HTTP{(int)response.StatusCode}" : problem.Type;
        }
        catch (JsonException)
        {
            return $"HTTP{(int)response.StatusCode}";
        }
    }

    private static OrgatexBatch ToBatch(SimBatch b) =>
        new(b.BatchId, b.OrderId, b.Machine, b.Fabric, b.BatchWeightKg, b.Shade, b.ComponentCount, b.TrainingOnly);

    private sealed record SimBatch(string BatchId, string OrderId, string Machine, string Fabric, decimal BatchWeightKg, string Shade, int ComponentCount, bool TrainingOnly);

    private sealed record SimComponent(int Sequence, string ChemicalId, string Code, string Name, string DisplayColour, decimal TargetQuantity, string Unit, decimal? Tolerance, string? SuggestedContainer);

    private sealed record SimConsumption(IReadOnlyList<SimConsumptionLine> Lines);

    private sealed record SimConsumptionLine(string ChemicalId, decimal ActualQuantity, string Unit, string Lot);

    private sealed record SimReceipt(string BatchId, string IdempotencyKey, DateTimeOffset ReceivedAt);

    private sealed record SimProblem(string? Type, string? Title);

    private sealed record SimRecipe(string BatchId, string Shade, bool TrainingOnly, IReadOnlyList<SimComponent> Components);
}
