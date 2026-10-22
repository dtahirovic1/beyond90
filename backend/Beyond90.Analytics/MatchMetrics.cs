namespace Beyond90.Analytics;

public enum PitchZone
{
    Defensive,
    Middle,
    Attacking
}

public sealed record MatchMetrics(
    int AttemptedPasses,
    int CompletedPasses,
    decimal? PassCompletionPercentage,
    IReadOnlyDictionary<PitchZone, int> TurnoversByZone,
    IReadOnlyDictionary<string, int> ShotsByInterval,
    int PressureEventCount);
