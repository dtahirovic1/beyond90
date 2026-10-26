namespace Beyond90.Domain;

public sealed class MatchPlayer
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DominantFoot? DominantFoot { get; set; }

    // Ratings
    public int? OverallRating { get; set; }
    public int? Potential { get; set; }
    public int? WeakFoot { get; set; }

    // Physical
    public int? Age { get; set; }
    public int? Pace { get; set; }
    public int? Acceleration { get; set; }
    public int? SprintSpeed { get; set; }
    public int? Strength { get; set; }
    public int? Stamina { get; set; }

    // Technical
    public int? Shooting { get; set; }
    public int? Finishing { get; set; }
    public int? Passing { get; set; }
    public int? Vision { get; set; }
    public int? Dribbling { get; set; }
    public int? BallControl { get; set; }
    public int? Defending { get; set; }
    public int? Interceptions { get; set; }
    public int? Composure { get; set; }

    // Season stats
    public int? MinutesPlayed { get; set; }
    public int? GoalsScored { get; set; }
    public int? Assists { get; set; }
    public decimal? ExpectedGoals { get; set; }
    public decimal? ExpectedAssists { get; set; }
}
