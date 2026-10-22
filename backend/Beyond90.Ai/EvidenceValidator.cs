using Beyond90.Domain;

namespace Beyond90.Ai;

public static class EvidenceValidator
{
    public static ValidationResult Validate(Insight insight, IReadOnlySet<string> knownEventIds, IReadOnlySet<string> knownMetricKeys)
    {
        ArgumentNullException.ThrowIfNull(insight);
        ArgumentNullException.ThrowIfNull(knownEventIds);
        ArgumentNullException.ThrowIfNull(knownMetricKeys);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(insight.InsightId))
            errors.Add("Insight ID is required.");
        if (string.IsNullOrWhiteSpace(insight.Text))
            errors.Add("Insight text is required.");
        if (insight.EventIds is null || insight.EventIds.Count == 0)
            errors.Add("At least one evidence event ID is required.");
        else
            foreach (var eventId in insight.EventIds)
                if (!knownEventIds.Contains(eventId))
                    errors.Add($"Insight references unknown or unselected event ID '{eventId}'.");

        if (insight.MetricEvidence is null)
        {
            errors.Add("Metric evidence cannot be null.");
        }
        else
        {
            foreach (var evidence in insight.MetricEvidence)
            {
                if (evidence is null)
                {
                    errors.Add("Metric evidence item cannot be null.");
                    continue;
                }
                if (!knownMetricKeys.Contains(evidence.MetricKey))
                    errors.Add($"Insight references unknown metric key '{evidence.MetricKey}'.");
                if (evidence.EventIds is null)
                {
                    errors.Add($"Metric evidence '{evidence.MetricKey}' has no event list.");
                    continue;
                }
                foreach (var eventId in evidence.EventIds)
                    if (!knownEventIds.Contains(eventId))
                        errors.Add($"Metric evidence references unknown or unselected event ID '{eventId}'.");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
