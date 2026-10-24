using System.Text.Json;
using Beyond90.Analytics;
using Beyond90.Domain;

namespace Beyond90.Api;

public sealed class MatchStore
{
    private readonly DemoMatch _demo;
    private IReadOnlyList<MatchStore>? _matches;
    public IReadOnlyList<MatchStore> All => _matches ?? new[] { this };
    public MatchStore? Find(string id) => All.FirstOrDefault(match => match.Id == id);
    public DecisionSnapshot? GetDecisionSnapshot(string sceneId) => _demo.DecisionSnapshots.FirstOrDefault(s => s.SceneId == sceneId);
    public static MatchStore LoadCatalog(string directory)
    {
        var matches = Directory.GetFiles(directory, "*.json").OrderBy(path => path, StringComparer.Ordinal).Select(Load).ToArray();
        if (matches.Length == 0) throw new InvalidDataException("No match scenarios available.");
        if (matches.Select(m => m.Id).Distinct().Count() != matches.Length) throw new InvalidDataException("Duplicate match IDs.");
        matches[0]._matches = matches;
        return matches[0];
    }

    private MatchStore(DemoMatch demo) => _demo = demo;

    public static MatchStore Load(string path)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var demo = JsonSerializer.Deserialize<DemoMatch>(File.ReadAllText(path), options)
            ?? throw new InvalidDataException("The synthetic match fixture is empty.");
        var match = new Match
        {
            Id = demo.MatchId,
            TeamIds = demo.Teams.Select(team => team.Id).ToList(),
            PlayerIds = demo.Players.Select(player => player.Id).ToList()
        };
        var validation = MatchEventValidator.Validate(match, demo.Events);
        if (!validation.IsValid)
            throw new InvalidDataException($"Synthetic match fixture is invalid: {string.Join(" ", validation.Errors)}");
        return new MatchStore(demo);
    }

    public string Id => _demo.MatchId;
    public IReadOnlyList<MatchTeam> Teams => _demo.Teams;
    public IReadOnlyList<MatchPlayer> Players => _demo.Players;
    public IReadOnlyList<MatchEvent> Events => _demo.Events;
    public IReadOnlyList<DecisionSnapshot> DecisionSnapshots => _demo.DecisionSnapshots;
    public MatchMetrics Metrics => MetricsCalculator.Calculate(_demo.Events);
    public IReadOnlyList<KeyMoment> KeyMoments => KeyMomentSelector.Select(_demo.Events, Metrics, 8);

    public string ScenarioLabel => _demo.ScenarioLabel;
    public object Summary => new
    {
        id = Id,
        scenarioLabel = ScenarioLabel,
        homeTeam = _demo.Teams.FirstOrDefault(team => team.Id == _demo.HomeTeamId)?.Name,
        awayTeam = _demo.Teams.FirstOrDefault(team => team.Id == _demo.AwayTeamId)?.Name,
        eventCount = _demo.Events.Count,
        dataStatus = _demo.DataStatus
    };

    public object Details => new
    {
        id = Id,
        scenarioLabel = ScenarioLabel,
        homeTeam = _demo.Teams.FirstOrDefault(team => team.Id == _demo.HomeTeamId),
        awayTeam = _demo.Teams.FirstOrDefault(team => team.Id == _demo.AwayTeamId),
        teams = _demo.Teams,
        players = _demo.Players,
        eventCount = _demo.Events.Count,
        dataStatus = _demo.DataStatus
    };

    private sealed class DemoMatch
    {
        public string ScenarioLabel { get; set; } = "Demo scenario";
        public string MatchId { get; set; } = string.Empty;
        public string HomeTeamId { get; set; } = string.Empty;
        public string AwayTeamId { get; set; } = string.Empty;
        public string DataStatus { get; set; } = string.Empty;
        public string RosterSeason { get; set; } = string.Empty;
        public string RosterSource { get; set; } = string.Empty;
        public string RosterSourceUrl { get; set; } = string.Empty;
        public List<MatchTeam> Teams { get; set; } = [];
        public List<MatchPlayer> Players { get; set; } = [];
        public List<MatchEvent> Events { get; set; } = [];
        public List<DecisionSnapshot> DecisionSnapshots { get; set; } = [];
    }
}
