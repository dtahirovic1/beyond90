namespace Beyond90.Domain;

public enum DominantFoot
{
    Left,
    Right,
    Both
}

public enum LaneCategory
{
    Open,
    Contested,
    Blocked
}

public sealed record PlayerPosition(string Id, string TeamId, int X, int Y, string? Role = null);

public sealed record BallPosition(int X, int Y);

public sealed record AttackDirection(string Arsenal, string Chelsea);

public sealed record PitchInfo(
    string CoordinateUnits,
    int LengthMeters,
    int WidthMeters,
    AttackDirection AttackDirection);

public sealed record MatchScore(int Arsenal, int Chelsea);

public sealed record MatchContext(
    int Minute,
    MatchScore Score,
    string? Phase,
    string? PossessionId);

public sealed record DecisionOption(
    string Id,
    string Type,
    string? ReceiverId);

public sealed record Decision(
    string PlayerId,
    string SelectedOptionId,
    string? BodyPartUsed,
    IReadOnlyList<DecisionOption> Options);

public sealed record PreDecisionState(
    int SecondsBeforeAction,
    BallPosition Ball,
    IReadOnlyList<PlayerPosition> Players);

public sealed record DecisionSnapshot(
    string SceneId,
    MatchContext MatchContext,
    PitchInfo Pitch,
    PreDecisionState PreDecision,
    Decision Decision,
    string? RecordedOutcome);