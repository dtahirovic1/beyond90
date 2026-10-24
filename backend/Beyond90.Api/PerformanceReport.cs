using Beyond90.Analytics;
using Beyond90.Domain;
namespace Beyond90.Api;

public sealed record PerformanceStats(int Events, int Passes, int CompletedPasses, decimal? PassAccuracy, int Shots, int Goals, int Recoveries, int TacklesWon, int Turnovers, int PressureEvents);
public static class PerformanceReport
{
    public static PerformanceStats Stats(IReadOnlyList<MatchEvent> events)
    {
        var metrics = MetricsCalculator.Calculate(events);
        bool Is(MatchEvent e, string type, string? outcome = null) => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase) && (outcome is null || string.Equals(e.Outcome, outcome, StringComparison.OrdinalIgnoreCase));
        return new(events.Count, metrics.AttemptedPasses, metrics.CompletedPasses, metrics.PassCompletionPercentage,
            events.Count(e => Is(e, "Shot")), events.Count(e => Is(e, "Shot", "Goal")), events.Count(e => Is(e, "Recovery", "Won")),
            events.Count(e => Is(e, "Tackle", "Won")), events.Count(e => Is(e, "PossessionChange", "Turnover")), metrics.PressureEventCount);
    }
    public static string[] Observations(PerformanceStats stats) => [
        stats.PassAccuracy is { } accuracy ? $"{stats.CompletedPasses} of {stats.Passes} recorded passes were completed ({accuracy:0.#}%). Review the incomplete passes before drawing conclusions about decision quality." : "No passes with known outcomes are available in this sample.",
        $"Recorded shooting: {stats.Shots} shots; {stats.Goals} goals. Open the shot events to review their outcomes and recorded locations.",
        $"Defensive actions: {stats.Recoveries} recoveries; {stats.TacklesWon} tackles won; {stats.Turnovers} turnovers. Start by reviewing the lost possessions and the pressure recorded at each action."
    ];
}
