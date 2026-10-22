using Beyond90.Analytics;
using Beyond90.Domain;

namespace Beyond90.Analytics.Tests;

public sealed class KeyMomentSelectorTests
{
    [Fact]
    public void Select_returns_stably_ordered_moments_with_known_event_evidence_and_respects_limit()
    {
        var events = new[]
        {
            Event("shot-1", 1, "Shot", 20),
            Event("turnover-1", 2, "PossessionChange", 30, "Turnover", true),
            Event("change-1", 3, "PossessionChange", 40, "Won"),
            Event("shot-2", 4, "Shot", 50)
        };
        var metrics = MetricsCalculator.Calculate(events);

        var moments = KeyMomentSelector.Select(events, metrics, 3);

        Assert.Equal(3, moments.Count);
        Assert.Equal(new[] { "turnover-1", "shot-1", "shot-2" }, moments.Select(moment => moment.EventIds.Single()));
        Assert.All(moments, moment => Assert.Contains(moment.EventIds.Single(), events.Select(e => e.EventId)));
        Assert.All(moments, moment => Assert.NotEmpty(moment.MetricEvidence));
        Assert.Equal(moments.Select(moment => moment.EventIds.Single()), KeyMomentSelector.Select(events, metrics, 3).Select(moment => moment.EventIds.Single()));
    }

    [Fact]
    public void Select_returns_no_more_than_requested_and_handles_non_positive_limit()
    {
        var events = new[]
        {
            Event("shot-1", 1, "Shot", 20),
            Event("change-1", 2, "PossessionChange", 30, "Won"),
            Event("shot-2", 3, "Shot", 40)
        };
        var metrics = MetricsCalculator.Calculate(events);

        Assert.Single(KeyMomentSelector.Select(events, metrics, 1));
        Assert.Empty(KeyMomentSelector.Select(events, metrics, 0));
    }

    private static MatchEvent Event(string id, int sequence, string type, int x, string? outcome = null, bool? underPressure = null) => new()
    {
        MatchId = "demo",
        EventId = id,
        Sequence = sequence,
        Minute = sequence * 3,
        TeamId = "home",
        PlayerId = "p1",
        Type = type,
        X = x,
        Y = 50,
        Outcome = outcome,
        UnderPressure = underPressure
    };
}
