using System.Text.Json;
using Beyond90.Analytics;
using Beyond90.Domain;

namespace Beyond90.Analytics.Tests;

public sealed class SyntheticMatchFixtureTests
{
    [Fact]
    public void Demo_fixture_validates_and_deserializes_to_the_same_ordered_event_list_each_time()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "match-demo.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var first = JsonSerializer.Deserialize<DemoFixture>(File.ReadAllText(path), options)!;
        var second = JsonSerializer.Deserialize<DemoFixture>(File.ReadAllText(path), options)!;
        var match = new Match
        {
            Id = first.MatchId,
            TeamIds = first.Teams.Select(team => team.Id).ToList(),
            PlayerIds = first.Players.Select(player => player.Id).ToList()
        };

        Assert.True(MatchEventValidator.Validate(match, first.Events).IsValid);
        Assert.Equal(first.Events.Select(e => e.EventId), second.Events.Select(e => e.EventId));
        Assert.Equal(first.Events.Select(e => e.Sequence), second.Events.Select(e => e.Sequence));
        Assert.True(first.Events.Count >= 10);
        var metrics = MetricsCalculator.Calculate(first.Events);
        Assert.True(KeyMomentSelector.Select(first.Events, metrics, 10).Count >= 3);
    }

    private sealed class DemoFixture
    {
        public string MatchId { get; set; } = string.Empty;
        public List<MatchTeam> Teams { get; set; } = [];
        public List<MatchPlayer> Players { get; set; } = [];
        public List<MatchEvent> Events { get; set; } = [];
    }
}
