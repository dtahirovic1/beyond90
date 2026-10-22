using Beyond90.Domain;

namespace Beyond90.Domain.Tests;

public class MatchEventValidatorTests
{
    [Fact]
    public void AcceptsValidEvents()
    {
        var result = Validate(Event("event-1", 1, "player-1", 20, 40));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsDuplicateEventIds()
    {
        var result = Validate(
            Event("event-1", 1, "player-1", 20, 40),
            Event("event-1", 2, "player-2", 25, 45));
        AssertHasError(result, "duplicate");
    }

    [Fact]
    public void RejectsDecreasingSequence()
    {
        var result = Validate(
            Event("event-1", 2, "player-1", 20, 40),
            Event("event-2", 1, "player-2", 25, 45));
        AssertHasError(result, "sequence");
    }

    [Fact]
    public void RejectsDecreasingTimestamp()
    {
        var result = Validate(
            Event("event-1", 1, "player-1", 20, 40, minute: 11),
            Event("event-2", 2, "player-2", 25, 45, minute: 10));
        AssertHasError(result, "timestamp order");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RejectsCoordinatesOutsidePitch(int x)
    {
        var result = Validate(Event("event-1", 1, "player-1", x, 40));
        AssertHasError(result, "coordinates");
    }

    [Fact]
    public void RejectsUnknownPlayer()
    {
        var result = Validate(Event("event-1", 1, "unknown-player", 20, 40));
        AssertHasError(result, "player");
    }

    [Fact]
    public void RejectsUnknownTeam()
    {
        var result = Validate(Event("event-1", 1, "player-1", 20, 40, teamId: "unknown-team"));
        AssertHasError(result, "team");
    }

    private static ValidationResult Validate(params MatchEvent[] events) => MatchEventValidator.Validate(
        new Match
        {
            Id = "match-1",
            TeamIds = ["home", "away"],
            PlayerIds = ["player-1", "player-2"]
        },
        events);

    private static MatchEvent Event(
        string eventId,
        int sequence,
        string playerId,
        int x,
        int y,
        int minute = 10,
        int second = 0,
        string teamId = "home") => new()
    {
        MatchId = "match-1",
        EventId = eventId,
        Sequence = sequence,
        Minute = minute,
        Second = second,
        TeamId = teamId,
        PlayerId = playerId,
        Type = "Pass",
        X = x,
        Y = y,
        Outcome = "Successful"
    };

    private static void AssertHasError(ValidationResult result, string text)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}
