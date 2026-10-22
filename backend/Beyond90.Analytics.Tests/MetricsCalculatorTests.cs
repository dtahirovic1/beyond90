using Beyond90.Analytics;
using Beyond90.Domain;

namespace Beyond90.Analytics.Tests;

public sealed class MetricsCalculatorTests
{
    [Fact]
    public void Calculate_counts_passes_and_returns_null_percentage_when_outcomes_are_missing()
    {
        var metrics = MetricsCalculator.Calculate([
            Event("e1", 1, "Pass", x: 20, outcome: null),
            Event("e2", 2, "Shot", x: 80)
        ]);

        Assert.Equal(1, metrics.AttemptedPasses);
        Assert.Equal(0, metrics.CompletedPasses);
        Assert.Null(metrics.PassCompletionPercentage);
    }

    [Fact]
    public void Calculate_buckets_turnovers_by_pitch_zone()
    {
        var metrics = MetricsCalculator.Calculate([
            Event("e1", 1, "PossessionChange", x: 20, outcome: "Turnover"),
            Event("e2", 2, "PossessionChange", x: 50, outcome: "Turnover"),
            Event("e3", 3, "PossessionChange", x: 80, outcome: "Turnover"),
            Event("e4", 4, "Pass", x: 50)
        ]);

        Assert.Equal(1, metrics.TurnoversByZone[PitchZone.Defensive]);
        Assert.Equal(1, metrics.TurnoversByZone[PitchZone.Middle]);
        Assert.Equal(1, metrics.TurnoversByZone[PitchZone.Attacking]);
    }

    [Fact]
    public void Calculate_counts_shots_in_fifteen_minute_intervals_and_pressure_events()
    {
        var metrics = MetricsCalculator.Calculate([
            Event("e1", 1, "Shot", minute: 14, underPressure: true),
            Event("e2", 2, "Shot", minute: 15, underPressure: false),
            Event("e3", 3, "Shot", minute: 30, underPressure: true),
            Event("e4", 4, "Pass", minute: 30, underPressure: true)
        ]);

        Assert.Equal(1, metrics.ShotsByInterval["00-14"]);
        Assert.Equal(1, metrics.ShotsByInterval["15-29"]);
        Assert.Equal(1, metrics.ShotsByInterval["30-44"]);
        Assert.Equal(3, metrics.PressureEventCount);
    }

    [Fact]
    public void Calculate_reports_pass_completion_from_known_outcomes_only()
    {
        var metrics = MetricsCalculator.Calculate([
            Event("e1", 1, "Pass", outcome: "completed"),
            Event("e2", 2, "Pass", outcome: "incomplete"),
            Event("e3", 3, "Pass", outcome: null)
        ]);

        Assert.Equal(3, metrics.AttemptedPasses);
        Assert.Equal(1, metrics.CompletedPasses);
        Assert.Equal(50m, metrics.PassCompletionPercentage);
    }

    private static MatchEvent Event(string id, int sequence, string type, int minute = 0, int x = 50, string? outcome = null, bool? underPressure = null) => new()
    {
        MatchId = "demo",
        EventId = id,
        Sequence = sequence,
        Minute = minute,
        TeamId = "home",
        PlayerId = "p1",
        Type = type,
        X = x,
        Y = 50,
        Outcome = outcome,
        UnderPressure = underPressure
    };
}
