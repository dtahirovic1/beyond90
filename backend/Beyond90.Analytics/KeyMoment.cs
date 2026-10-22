using Beyond90.Domain;

namespace Beyond90.Analytics;

public sealed record KeyMoment(
    IReadOnlyList<string> EventIds,
    int Score,
    string ReasonCode,
    IReadOnlyList<MetricEvidence> MetricEvidence);
