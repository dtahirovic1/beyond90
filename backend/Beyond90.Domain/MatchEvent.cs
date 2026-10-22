namespace Beyond90.Domain;

public sealed class MatchEvent
{
    public string MatchId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
    public string TeamId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public string? Outcome { get; set; }
    public bool? UnderPressure { get; set; }
    public List<string>? ReceiverOptions { get; set; }
}
