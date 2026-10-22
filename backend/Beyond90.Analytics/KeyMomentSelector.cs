using Beyond90.Domain;

namespace Beyond90.Analytics;

public static class KeyMomentSelector
{
    public static IReadOnlyList<KeyMoment> Select(IReadOnlyList<MatchEvent> events, MatchMetrics metrics, int limit)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(metrics);
        if (limit <= 0)
            return [];

        var candidates = new List<(MatchEvent Event, KeyMoment Moment)>();
        foreach (var matchEvent in events)
        {
            var (score, reasonCode, metricKey) = Classify(matchEvent);
            if (score == 0)
                continue;

            candidates.Add((matchEvent, new KeyMoment(
                [matchEvent.EventId],
                score,
                reasonCode,
                [new MetricEvidence { MetricKey = metricKey, Value = 1, EventIds = [matchEvent.EventId] }])));
        }

        return candidates
            .OrderByDescending(candidate => candidate.Moment.Score)
            .ThenBy(candidate => candidate.Event.Sequence)
            .Take(limit)
            .Select(candidate => candidate.Moment)
            .ToArray();
    }

    private static (int Score, string ReasonCode, string MetricKey) Classify(MatchEvent matchEvent)
    {
        if (IsType(matchEvent, "Shot"))
            return (80, "shot", "shots.event");

        if (IsType(matchEvent, "PossessionChange") && string.Equals(matchEvent.Outcome, "Turnover", StringComparison.OrdinalIgnoreCase))
            return matchEvent.UnderPressure == true
                ? (90, "pressured_turnover", "turnovers.pressured")
                : (70, "turnover", "turnovers.event");

        if (IsType(matchEvent, "PossessionChange"))
            return (55, "possession_change", "possession.changes");

        return (0, string.Empty, string.Empty);
    }

    private static bool IsType(MatchEvent matchEvent, string type) =>
        string.Equals(matchEvent.Type, type, StringComparison.OrdinalIgnoreCase);
}
