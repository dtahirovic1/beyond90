namespace Beyond90.Domain;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class MatchEventValidator
{
    public static ValidationResult Validate(Match match, IReadOnlyList<MatchEvent> events)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(events);

        var errors = new List<string>();
        var eventIds = new HashSet<string>(StringComparer.Ordinal);
        var teamIds = match.TeamIds.ToHashSet(StringComparer.Ordinal);
        var playerIds = match.PlayerIds.ToHashSet(StringComparer.Ordinal);
        var previousSequence = int.MinValue;
        var previousTimeSeconds = -1;

        if (string.IsNullOrWhiteSpace(match.Id))
            errors.Add("Match ID is required.");

        foreach (var matchEvent in events)
        {
            if (string.IsNullOrWhiteSpace(matchEvent.EventId))
                errors.Add("Event ID is required.");
            else if (!eventIds.Add(matchEvent.EventId))
                errors.Add($"Duplicate event ID '{matchEvent.EventId}'.");

            if (!string.Equals(match.Id, matchEvent.MatchId, StringComparison.Ordinal))
                errors.Add($"Event '{matchEvent.EventId}' references a different match.");

            if (matchEvent.Sequence <= previousSequence)
                errors.Add($"Event '{matchEvent.EventId}' has a non-increasing sequence.");
            previousSequence = matchEvent.Sequence;

            if (matchEvent.Minute < 0 || matchEvent.Second is < 0 or > 59)
                errors.Add($"Event '{matchEvent.EventId}' has an invalid timestamp.");
            else
            {
                var timeSeconds = matchEvent.Minute * 60 + matchEvent.Second;
                if (timeSeconds < previousTimeSeconds)
                    errors.Add($"Event '{matchEvent.EventId}' is out of timestamp order.");
                previousTimeSeconds = timeSeconds;
            }

            if (matchEvent.X is < 0 or > 100 || matchEvent.Y is < 0 or > 100)
                errors.Add($"Event '{matchEvent.EventId}' has coordinates outside the 0-100 pitch bounds.");

            if (!teamIds.Contains(matchEvent.TeamId))
                errors.Add($"Event '{matchEvent.EventId}' references an unknown team.");

            if (!playerIds.Contains(matchEvent.PlayerId))
                errors.Add($"Event '{matchEvent.EventId}' references an unknown player.");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
