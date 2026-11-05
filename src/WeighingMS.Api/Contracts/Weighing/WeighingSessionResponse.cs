namespace WeighingMS.Api.Contracts.Weighing;

/// <summary>A weighing session: progress, the suggested and active component, and every component.</summary>
public sealed record WeighingSessionResponse(
    string BatchId,
    string Status,
    DateTimeOffset StartedAt,
    string StartedBy,
    ProgressResponse Progress,
    int? SuggestedSequence,
    int? ActiveSequence,
    IReadOnlyList<WeighingComponentResponse> Components);

/// <summary>"n of N" components done.</summary>
public sealed record ProgressResponse(int Done, int Total);
