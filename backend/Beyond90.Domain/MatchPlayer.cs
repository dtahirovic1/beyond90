namespace Beyond90.Domain;

public sealed class MatchPlayer
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DominantFoot? DominantFoot { get; set; }
}
