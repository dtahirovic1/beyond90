using Beyond90.Analytics;
using Beyond90.Domain;
namespace Beyond90.Api;

public sealed record PerformanceStats(int Events, int Passes, int CompletedPasses, decimal? PassAccuracy, int Shots, int Goals, int Recoveries, int TacklesWon, int Turnovers, int PressureEvents);
public static class PerformanceReport
{
    public static object Segments(IReadOnlyList<MatchEvent> events)
    {
        object Passing(IEnumerable<MatchEvent> source)
        {
            var passes=source.Where(e=>e.Type=="Pass").ToArray();
            var known=passes.Where(e=>e.Outcome is "Completed" or "Incomplete").ToArray();
            var completed=known.Count(e=>e.Outcome=="Completed");
            return new { attempts=passes.Length, knownOutcomes=known.Length, completed,
                completionPercent=known.Length==0 ? (decimal?)null : Math.Round(100m*completed/known.Length,1) };
        }
        var shots=events.Where(e=>e.Type=="Shot").ToArray();
        var tackles=events.Where(e=>e.Type=="Tackle").ToArray();
        return new {
            passing=new { overall=Passing(events), underPressure=Passing(events.Where(e=>e.UnderPressure==true)), withoutPressure=Passing(events.Where(e=>e.UnderPressure==false)), unknownPressure=Passing(events.Where(e=>e.UnderPressure is null)) },
            shooting=new { attempts=shots.Length,goals=shots.Count(e=>e.Outcome=="Goal"),unknownOutcomes=shots.Count(e=>e.Outcome is null) },
            defending=new { tackles=tackles.Length,won=tackles.Count(e=>e.Outcome=="Won"),lost=tackles.Count(e=>e.Outcome=="Lost"),recoveries=events.Count(e=>e.Type=="Recovery" && e.Outcome=="Won") },
            possession=new { recordedTurnovers=events.Count(e=>e.Type=="PossessionChange" && e.Outcome=="Turnover"),turnoversUnderPressure=events.Count(e=>e.Type=="PossessionChange" && e.Outcome=="Turnover" && e.UnderPressure==true) },
            scope="Selected player event sample; no full-team possession, defensive shape, pressing effectiveness or chance-quality measures. Counts are not per-90 rates."
        };
    }
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
