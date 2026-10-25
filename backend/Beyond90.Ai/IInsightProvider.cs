using Beyond90.Domain;

namespace Beyond90.Ai;

public sealed record InsightRequest(
    string MatchId,
    IReadOnlyList<string> EventIds,
    Audience Audience,
    IReadOnlyDictionary<string, decimal?> VerifiedMetrics,
    IReadOnlyList<MatchEvent> SelectedEvents)
{
    public IReadOnlyList<MatchPlayer> Players { get; init; } = [];
    public IReadOnlyList<MatchTeam> Teams { get; init; } = [];
    public IReadOnlyList<Beyond90.Analytics.DecisionAnalysis> Decisions { get; init; } = [];
    public object? ReportContext { get; init; }
    public object? DecisionFocus { get; init; }
    public int? RecordedEventCount { get; init; }
    public int? PassesWithKnownOutcome { get; init; }
}

public interface IInsightProvider
{
    Task<Insight> GenerateAsync(InsightRequest request, CancellationToken cancellationToken);
}
