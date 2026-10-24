namespace Beyond90.Domain;

// Explicit, complete goal log for a fictional match, separate from the sampled actions.
public sealed record MatchGoal(string EventId, string TeamId, int Minute, int Second);
public sealed record MatchResult(int Home, int Away);
public static class MatchScore
{
    public static Dictionary<string, int> Before(IEnumerable<MatchGoal> goals, IEnumerable<string> teams, int minute, int second)
        => teams.ToDictionary(t => t, t => goals.Count(g => g.TeamId == t && (g.Minute < minute || g.Minute == minute && g.Second < second)));
}
