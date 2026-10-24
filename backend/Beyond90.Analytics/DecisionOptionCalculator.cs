using System.Linq;
using Beyond90.Domain;

namespace Beyond90.Analytics;

public sealed record PassOptionAssessment(
    string OptionId,
    decimal LengthMeters,
    decimal AngleDegrees,
    decimal NearestOpponentDistanceMeters,
    LaneCategory Lane,
    string Reason);

public sealed record ShotOptionAssessment(
    string OptionId,
    decimal DistanceToGoalMeters,
    decimal AngleToGoalDegrees,
    int OpponentsBetween,
    LaneCategory Lane,
    string Reason);

public sealed record DribbleOptionAssessment(
    string OptionId,
    decimal SpaceAheadMeters,
    int OpponentCount,
    decimal NearestOpponentDistanceMeters,
    LaneCategory Lane,
    string Reason);

public static class DecisionOptionCalculator
{
    private const int PitchLengthMeters = 105;
    private const int PitchWidthMeters = 68;

    public static PassOptionAssessment CalculatePassOption(
        PlayerPosition passer,
        PlayerPosition receiver,
        IReadOnlyList<PlayerPosition> opponents)
    {
        if (passer is null) throw new ArgumentNullException(nameof(passer));
        if (receiver is null) throw new ArgumentNullException(nameof(receiver));
        if (opponents is null) throw new ArgumentNullException(nameof(opponents));

        var lengthMeters = DistanceMeters(passer.X, passer.Y, receiver.X, receiver.Y);
        var angleDegrees = AngleDegrees(passer.X, passer.Y, receiver.X, receiver.Y);
        var nearestOpponentDistance = opponents.Count == 0
            ? decimal.MaxValue
            : opponents.Min(o => DistanceMeters(receiver.X, receiver.Y, o.X, o.Y));

        var lane = ClassifyLane(nearestOpponentDistance, lengthMeters);
        var reason = lane switch
        {
            LaneCategory.Open => $"Open passing lane to {receiver.Id}; nearest opponent is {nearestOpponentDistance:0.#}m away.",
            LaneCategory.Contested => $"Contested pass to {receiver.Id}; nearest opponent is {nearestOpponentDistance:0.#}m away.",
            LaneCategory.Blocked => $"Blocked pass to {receiver.Id}; nearest opponent is {nearestOpponentDistance:0.#}m away.",
            _ => "Unable to assess lane."
        };

        return new PassOptionAssessment(
            $"pass-{receiver.Id}",
            lengthMeters,
            angleDegrees,
            nearestOpponentDistance == decimal.MaxValue ? 0 : nearestOpponentDistance,
            lane,
            reason);
    }

    public static ShotOptionAssessment CalculateShotOption(
        PlayerPosition shooter,
        string attackingTeamId,
        IReadOnlyList<PlayerPosition> opponents)
    {
        if (shooter is null) throw new ArgumentNullException(nameof(shooter));
        if (opponents is null) throw new ArgumentNullException(nameof(opponents));

        var goalX = attackingTeamId == "leeds" ? 105 : 0;
        var goalY = PitchWidthMeters / 2;

        var distanceToGoal = DistanceMeters(shooter.X, shooter.Y, goalX, goalY);
        var angleToGoal = AngleDegrees(shooter.X, shooter.Y, goalX, goalY);
        var opponentsBetween = opponents.Count(o =>
            IsBetween(shooter.X, shooter.Y, goalX, goalY, o.X, o.Y));

        var lane = opponentsBetween switch
        {
            0 => LaneCategory.Open,
            1 => LaneCategory.Contested,
            _ => LaneCategory.Blocked
        };

        var reason = lane switch
        {
            LaneCategory.Open => $"Open shot lane; {distanceToGoal:0.#}m from goal.",
            LaneCategory.Contested => $"Contested shot; {opponentsBetween} opponent between ball and goal.",
            LaneCategory.Blocked => $"Blocked shot; {opponentsBetween} opponents between ball and goal.",
            _ => "Unable to assess shot lane."
        };

        return new ShotOptionAssessment(
            "shot",
            distanceToGoal,
            angleToGoal,
            opponentsBetween,
            lane,
            reason);
    }

    public static DribbleOptionAssessment CalculateDribbleOption(
        PlayerPosition ballCarrier,
        string attackingTeamId,
        IReadOnlyList<PlayerPosition> opponents)
    {
        if (ballCarrier is null) throw new ArgumentNullException(nameof(ballCarrier));
        if (opponents is null) throw new ArgumentNullException(nameof(opponents));

        var direction = attackingTeamId == "leeds" ? 1 : -1;
        var spaceAheadMeters = direction > 0
            ? PitchLengthMeters - ballCarrier.X
            : ballCarrier.X;

        var opponentCount = opponents.Count(o =>
            Math.Abs(o.X - ballCarrier.X) < 15 && Math.Abs(o.Y - ballCarrier.Y) < 15);

        var nearestOpponentDistance = opponents.Count == 0
            ? decimal.MaxValue
            : opponents.Min(o => DistanceMeters(ballCarrier.X, ballCarrier.Y, o.X, o.Y));

        var lane = opponentCount switch
        {
            0 => LaneCategory.Open,
            1 => LaneCategory.Contested,
            _ => LaneCategory.Blocked
        };

        var reason = lane switch
        {
            LaneCategory.Open => $"Open space ahead ({spaceAheadMeters:0.#}m to goal line).",
            LaneCategory.Contested => $"Contested dribble; {opponentCount} opponent nearby.",
            LaneCategory.Blocked => $"Blocked dribble; {opponentCount} opponents nearby.",
            _ => "Unable to assess dribble lane."
        };

        return new DribbleOptionAssessment(
            "carry",
            spaceAheadMeters,
            opponentCount,
            nearestOpponentDistance == decimal.MaxValue ? 0 : nearestOpponentDistance,
            lane,
            reason);
    }

    private static decimal DistanceMeters(int x1, int y1, int x2, int y2)
    {
        var dx = (x2 - x1) / 100m * PitchLengthMeters;
        var dy = (y2 - y1) / 100m * PitchWidthMeters;
        return (decimal)Math.Sqrt((double)(dx * dx + dy * dy));
    }

    private static decimal AngleDegrees(int x1, int y1, int x2, int y2)
    {
        var dx = (x2 - x1) / 100m * PitchLengthMeters;
        var dy = (y2 - y1) / 100m * PitchWidthMeters;
        var radians = (decimal)Math.Atan2((double)dy, (double)dx);
        var degrees = radians * 180m / (decimal)Math.PI;
        return degrees < 0 ? degrees + 360m : degrees;
    }

    private static bool IsBetween(int x1, int y1, int x2, int y2, int ox, int oy)
    {
        var lineLength = DistanceMeters(x1, y1, x2, y2);
        var distToOpponent = DistanceMeters(x1, y1, ox, oy);
        var distFromOpponentToEnd = DistanceMeters(ox, oy, x2, y2);
        return distToOpponent < lineLength && distFromOpponentToEnd < lineLength &&
               Math.Abs(distToOpponent + distFromOpponentToEnd - lineLength) < 5m;
    }

    private static LaneCategory ClassifyLane(decimal nearestOpponentDistance, decimal passLength)
    {
        if (nearestOpponentDistance >= 8m) return LaneCategory.Open;
        if (nearestOpponentDistance >= 4m) return LaneCategory.Contested;
        return LaneCategory.Blocked;
    }
}