namespace Beyond90.Domain;

public sealed class Match
{
    public string Id { get; set; } = string.Empty;
    public List<string> TeamIds { get; set; } = [];
    public List<string> PlayerIds { get; set; } = [];
}
