namespace Beyond90.Domain;
public enum DominantFoot { Left, Right, Both }
public enum LaneCategory { Open, Contested, Blocked }
public sealed record PlayerPosition(string Id, string TeamId, int X, int Y, string? Role = null);
public sealed record BallPosition(int X, int Y);
public sealed record PitchInfo(string CoordinateUnits, int LengthMeters, int WidthMeters, IReadOnlyDictionary<string,string> AttackDirection);
public sealed record MatchContext(int Minute, int Second, IReadOnlyDictionary<string,int>? Score, string? Phase, string? PossessionId);
public sealed record DecisionOption(string Id, string Type, string? ReceiverId = null, string? BodyPart = null);
public sealed record Decision(string PlayerId, string SelectedOptionId, string? BodyPartUsed, IReadOnlyList<DecisionOption> Options);
public sealed record PreDecisionState(int SecondsBeforeAction, BallPosition Ball, IReadOnlyList<PlayerPosition> Players,
    string? BodySetup = null, string? BallHeight = null);
public sealed record DecisionSnapshot(string SceneId, string MatchId, string EventId, MatchContext MatchContext, PitchInfo Pitch, PreDecisionState PreDecision, Decision Decision, string? RecordedOutcome);
