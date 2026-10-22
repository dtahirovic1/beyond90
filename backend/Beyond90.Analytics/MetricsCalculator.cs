using Beyond90.Domain;

namespace Beyond90.Analytics;

public static class MetricsCalculator
{
    public static MatchMetrics Calculate(IReadOnlyList<MatchEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var passes = events.Where(e => IsType(e, "Pass")).ToArray();
        var knownOutcomes = passes.Where(e => !string.IsNullOrWhiteSpace(e.Outcome)).ToArray();
        var completedPasses = knownOutcomes.Count(e => string.Equals(e.Outcome, "completed", StringComparison.OrdinalIgnoreCase));
        decimal? completionPercentage = knownOutcomes.Length == 0
            ? null
            : decimal.Round(completedPasses * 100m / knownOutcomes.Length, 1);

        var turnovers = events
            .Where(e => IsType(e, "PossessionChange") && string.Equals(e.Outcome, "turnover", StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => ZoneFor(e.X))
            .ToDictionary(group => group.Key, group => group.Count());

        var shots = events
            .Where(e => IsType(e, "Shot"))
            .GroupBy(e => e.Minute / 15)
            .OrderBy(group => group.Key)
            .ToDictionary(group => IntervalName(group.Key), group => group.Count());

        var pressureEventCount = events.Count(e => IsType(e, "Pressure") || e.UnderPressure == true);

        return new MatchMetrics(passes.Length, completedPasses, completionPercentage, turnovers, shots, pressureEventCount);
    }

    public static PitchZone ZoneFor(int x) => x switch
    {
        < 34 => PitchZone.Defensive,
        < 67 => PitchZone.Middle,
        _ => PitchZone.Attacking
    };

    private static bool IsType(MatchEvent matchEvent, string type) =>
        string.Equals(matchEvent.Type, type, StringComparison.OrdinalIgnoreCase);

    private static string IntervalName(int intervalIndex)
    {
        var start = intervalIndex * 15;
        return $"{start:00}-{start + 14:00}";
    }
}
