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
    public static MatchStore LoadCatalog(string directory, bool includeCity = false)
    {
        var matches = new List<MatchStore>();
    foreach (var path in Directory.GetFiles(directory, "*.json").Concat(includeCity && Directory.Exists(Path.Combine(directory, "..", "city")) ? Directory.GetFiles(Path.Combine(directory, "..", "city"), "*.json") : Array.Empty<string>()).OrderBy(p => p, StringComparer.Ordinal))
    {
        try
        {
            matches.Add(Load(path));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: skipping synthetic match fixture '{path}': {ex.Message}");
            Console.WriteLine($"STACK: {ex.StackTrace}");
        }
    }
        if (matches.Count == 0) throw new InvalidDataException("No match scenarios available.");
        if (matches.Select(m => m.Id).Distinct().Count() != matches.Count) throw new InvalidDataException("Duplicate match IDs.");
        matches[0]._matches = matches;
        return matches[0];
    }

    private MatchStore(DemoMatch demo) => _demo = demo;

    public static MatchStore Load(string path)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
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
        if (demo.Goals is not null || demo.Result is not null)
        {
            if (demo.Goals is null || demo.Result is null || !demo.GoalLogComplete)
                throw new InvalidDataException("A result requires a declared complete synthetic goal log.");
            var teamIds=demo.Teams.Select(t=>t.Id).ToArray();
            if(demo.Goals.Select(g=>g.EventId).Distinct().Count()!=demo.Goals.Count ||
                demo.Goals.Any(g=>!teamIds.Contains(g.TeamId) || g.Minute<0 || g.Second<0 || g.Second>59) ||
                demo.Result.Home!=demo.Goals.Count(g=>g.TeamId==demo.HomeTeamId) || demo.Result.Away!=demo.Goals.Count(g=>g.TeamId==demo.AwayTeamId))
                throw new InvalidDataException("Synthetic result and goal log disagree.");
            foreach(var e in demo.Events)
            {
                var goal=demo.Goals.FirstOrDefault(g=>g.EventId==e.EventId);
                if((e.Outcome=="Goal") != (goal is not null) || goal is not null && (goal.TeamId!=e.TeamId || goal.Minute!=e.Minute || goal.Second!=e.Second))
                    throw new InvalidDataException("Recorded goal and goal log disagree.");
                e.ScoreBefore=MatchScore.Before(demo.Goals,teamIds,e.Minute,e.Second);
            }
            foreach(var scene in demo.DecisionSnapshots)
            {
                var expected=MatchScore.Before(demo.Goals,teamIds,scene.MatchContext.Minute,scene.MatchContext.Second);
                if(scene.MatchContext.Score is not null && (scene.MatchContext.Score.Count!=expected.Count || expected.Any(p=>!scene.MatchContext.Score.TryGetValue(p.Key,out var value)||value!=p.Value)))
                    throw new InvalidDataException("Scene score must describe the score before the action.");
            }
            demo.DecisionSnapshots=demo.DecisionSnapshots.Select(s=>s with { MatchContext=s.MatchContext with { Score=MatchScore.Before(demo.Goals,teamIds,s.MatchContext.Minute,s.MatchContext.Second) } }).ToList();
        }
        DecisionSnapshotValidator.Validate(demo.MatchId,demo.Teams,demo.Players,demo.Events,demo.DecisionSnapshots);
        return new MatchStore(demo);
    }

    public IReadOnlyList<DecisionAnalysis> Decisions => DecisionSnapshots.Select(s => ExecutionProfiles.Attach(DecisionOptionCalculator.Calculate(s), Players)).ToArray();
    public string Id => _demo.MatchId;
    public IReadOnlyList<MatchTeam> Teams => _demo.Teams;
    public IReadOnlyList<MatchPlayer> Players => _demo.Players;
    public IReadOnlyList<MatchEvent> Events => _demo.Events;
    public IReadOnlyList<DecisionSnapshot> DecisionSnapshots => _demo.DecisionSnapshots;
    public MatchMetrics Metrics => MetricsCalculator.Calculate(_demo.Events);
    public IReadOnlyList<KeyMoment> KeyMoments => KeyMomentSelector.Select(_demo.Events, Metrics, 8);

    public string ScenarioLabel => _demo.ScenarioLabel;
    public MatchResult? Result => _demo.Result;
    public object Summary => new
    {
        id = Id,
        scenarioLabel = ScenarioLabel,
        result = Result,
        homeTeam = _demo.Teams.FirstOrDefault(team => team.Id == _demo.HomeTeamId)?.Name,
        awayTeam = _demo.Teams.FirstOrDefault(team => team.Id == _demo.AwayTeamId)?.Name,
        eventCount = _demo.Events.Count,
        dataStatus = _demo.DataStatus
    };

    public object Details => new
    {
        id = Id,
        scenarioLabel = ScenarioLabel,
        result = Result,
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
        public List<MatchGoal>? Goals { get; set; }
        public bool GoalLogComplete { get; set; }
        public MatchResult? Result { get; set; }
    }
}
