using Beyond90.Domain;

namespace Beyond90.Ai;

public sealed record InsightRequest(
    string MatchId,
    IReadOnlyList<string> EventIds,
    Audience Audience,
    IReadOnlyDictionary<string, decimal?> VerifiedMetrics,
    IReadOnlyList<MatchEvent> SelectedEvents);

public interface IInsightProvider
{
    Task<Insight> GenerateAsync(InsightRequest request, CancellationToken cancellationToken);
}
