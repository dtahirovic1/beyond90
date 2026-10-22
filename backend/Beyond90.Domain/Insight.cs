namespace Beyond90.Domain;

public sealed class Insight
{
    public string InsightId { get; set; } = string.Empty;
    public Audience Audience { get; set; }
    public List<string> EventIds { get; set; } = [];
    public List<MetricEvidence> MetricEvidence { get; set; } = [];
    public string Text { get; set; } = string.Empty;
    public List<string> Caveats { get; set; } = [];
}

public sealed class MetricEvidence
{
    public string MetricKey { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public List<string> EventIds { get; set; } = [];
}
