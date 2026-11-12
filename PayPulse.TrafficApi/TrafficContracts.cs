internal sealed class StartTrafficRequest
{
    public int DurationSeconds { get; init; } = 60;
    public int RequestsPerSecond { get; init; } = 100;
    public int Concurrency { get; init; } = 20;
    public decimal Amount { get; init; } = 150m;

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (DurationSeconds is < 1 or > 120)
            errors[nameof(DurationSeconds)] = ["DurationSeconds must be between 1 and 120."];
        if (RequestsPerSecond is < 1 or > 200)
            errors[nameof(RequestsPerSecond)] = ["RequestsPerSecond must be between 1 and 200."];
        if (Concurrency is < 1 or > 100)
            errors[nameof(Concurrency)] = ["Concurrency must be between 1 and 100."];
        if (Amount <= 0 || Amount > 1_000_000)
            errors[nameof(Amount)] = ["Amount must be greater than 0 and at most 1000000."];
        return errors;
    }
}

internal sealed record TrafficRunSnapshot(
    Guid RunId,
    string Profile,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int DurationSeconds,
    double ProgressPercent,
    int RequestsPerSecond,
    int Concurrency,
    decimal Amount,
    TrafficStatisticsSnapshot Statistics,
    string? Error = null);

internal sealed record TrafficStatisticsSnapshot(
    long ScheduledRequests,
    long Responses,
    long SuccessfulResponses,
    long FailedResponses,
    long Errors,
    long CanceledRequests,
    long Approved,
    long Rejected,
    long Declined,
    double AchievedResponsesPerSecond,
    double? AverageLatencyMilliseconds,
    double? P50LatencyMilliseconds,
    double? P95LatencyMilliseconds,
    IReadOnlyDictionary<string, int> RequestsByRiskEngineInstance);
