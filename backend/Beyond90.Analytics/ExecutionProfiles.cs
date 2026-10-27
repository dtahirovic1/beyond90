using Beyond90.Domain;

namespace Beyond90.Analytics;

public sealed record ExecutionAttribute(string Key, string Label, int? Value, string Relevance);
public sealed record ExecutionProfile(IReadOnlyList<ExecutionAttribute> Attributes, string FootContext, string Note);

public static class ExecutionProfiles
{
    public static DecisionAnalysis Attach(DecisionAnalysis analysis, IReadOnlyList<MatchPlayer> players)
    {
        var player = players.FirstOrDefault(p => p.Id == analysis.Snapshot.Decision.PlayerId);
        return analysis with { Options = analysis.Options.Select(option => option with {
            ExecutionProfile = Create(player, analysis.Snapshot.Decision.Options.Single(o => o.Id == option.OptionId))
        }).ToArray() };
    }

    public static ExecutionProfile Create(MatchPlayer? player, DecisionOption option)
    {
        ExecutionAttribute Attribute(string key, string label, int? value, string relevance)
            => new(key, label, value is >= 0 and <= 100 ? value : null, relevance);
        ExecutionAttribute[] attributes = option.BodyPart == "head" ? [] : option.Type switch {
            "pass" => [Attribute("passing", "Passing", player?.Passing, "Delivery of the selected pass."),
                Attribute("vision", "Vision", player?.Vision, "Recognising a passing option; does not prove the player saw it.")],
            "shot" => [Attribute("shooting", "Shooting", player?.Shooting, "General shooting technique."),
                Attribute("finishing", "Finishing", player?.Finishing, "Finishing execution; not a scoring probability."),
                Attribute("composure", "Composure", player?.Composure, "Execution under pressure; not the player's observed mental state.")],
            "dribble" => [Attribute("dribbling", "Dribbling", player?.Dribbling, "Carrying the ball past pressure."),
                Attribute("ballControl", "Ball control", player?.BallControl, "Keeping the ball close during the carry."),
                Attribute("acceleration", "Acceleration", player?.Acceleration, "Initial burst; does not supply measured running speed.")],
            "tackle" => [Attribute("defending", "Defending", player?.Defending, "Defensive execution; timing is not measured."),
                Attribute("strength", "Strength", player?.Strength, "Physical contest; does not establish a duel outcome.")],
            _ => []
        };
        var foot = "Option foot unknown; no dominant-foot comparison available.";
        if (option.BodyPart is "left_foot" or "right_foot")
        {
            var side = option.BodyPart == "left_foot" ? DominantFoot.Left : DominantFoot.Right;
            foot = player?.DominantFoot switch {
                DominantFoot.Both => "Both feet listed as dominant in the synthetic profile.",
                null => "Dominant foot unknown; no foot comparison available.",
                var dominant when dominant == side => "Dominant foot selected in the synthetic profile.",
                _ => "Non-dominant foot selected." + (player.WeakFoot is >= 1 and <= 5
                    ? $" Synthetic weak-foot rating: {player.WeakFoot}/5." : " Weak-foot rating unavailable.")
            };
        }
        else if (option.BodyPart is not null) foot = "No foot comparison for this technique.";
        return new(attributes, foot, option.BodyPart == "head"
            ? "No heading attribute supplied; finishing is not a substitute for heading ability."
            : "Synthetic profile context only. These attributes do not change the spatial comparison or predict success.");
    }
}
