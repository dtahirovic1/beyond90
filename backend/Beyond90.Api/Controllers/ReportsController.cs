using Beyond90.Ai;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;
namespace Beyond90.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(MatchStore catalogue, IInsightProvider provider, DemoInsightProvider demo, ILogger<ReportsController>? logger=null) : ControllerBase
{
    [HttpGet("club")]
    public IActionResult Club([FromQuery] string teamId = "leeds") => teamId is "leeds" or "city"
        ? Ok(new { name = teamId == "city" ? "Manchester City" : "Leeds United", players = catalogue.All.SelectMany(m => m.Players).Where(p => p.TeamId == teamId).DistinctBy(p => p.Id) }) : NotFound();

    private MatchStore[]? Select(string? playerId, string? matchId, string teamId)
    {
        if (playerId is not null && !catalogue.All.SelectMany(m => m.Players).Any(p => p.TeamId == teamId && p.Id == playerId)) return null;
        if (teamId is not ("leeds" or "city")) return null;
        var matches = catalogue.All.Where(m => m.Teams[0].Id == teamId && (matchId is null || m.Id == matchId)).ToArray();
        return matches.Length == 0 ? null : matches;
    }
    private static MatchEvent[] Events(IEnumerable<MatchStore> matches, string? playerId, string teamId) => matches.SelectMany(m => m.Events).Where(e => e.TeamId == teamId && (playerId is null || e.PlayerId == playerId)).ToArray();

    [HttpGet]
    public IActionResult Get([FromQuery] string? playerId, [FromQuery] string? matchId, [FromQuery] string teamId = "leeds")
    {
        var matches = Select(playerId, matchId, teamId);
        if (matches is null) return NotFound();
        var stats = PerformanceReport.Stats(Events(matches, playerId, teamId));
        return Ok(new { playerId, matchId, matchCount = matches.Length, stats, observations = PerformanceReport.Observations(stats),
            coverage = "Synthetic event samples from selected players. Not complete match statistics or real player performance.",
            matches = matches.Select(m => new { id = m.Id, scenarioLabel = m.ScenarioLabel, opponent = m.Teams.First(t => t.Id != teamId).Name, result = m.Result, stats = PerformanceReport.Stats(Events([m], playerId, teamId)) }) });
    }

    [HttpPost("insight")]
    public async Task<IActionResult> Insight([FromBody] ReportInsightRequest request, CancellationToken cancellationToken)
    {
        if (request.Audience is not (Audience.Player or Audience.Coach)) return BadRequest(new { error = "Select Player or Coach." });
        if (request.Audience == Audience.Player && string.IsNullOrWhiteSpace(request.PlayerId)) return BadRequest(new { error = "Select a player." });
        var teamId = request.TeamId;
        var matches = Select(request.PlayerId, request.MatchId, teamId);
        if (matches is null) return NotFound();
        var events = Events(matches, request.PlayerId, teamId);
        if (events.Length == 0) return BadRequest(new { error = "No events available for this selection." });
        var stats = PerformanceReport.Stats(events);
        var input = new InsightRequest(request.MatchId ?? $"{teamId}-overview", events.Select(e => e.EventId).ToArray(), request.Audience,
            new Dictionary<string, decimal?> { ["events.selected"] = events.Length }, events)
        {
            Players = matches.SelectMany(m => m.Players).DistinctBy(p => p.Id).ToArray(),
            Teams = matches.SelectMany(m => m.Teams).DistinctBy(t => t.Id).ToArray(),
            Decisions = matches.SelectMany(m => m.Decisions).Where(d => events.Any(e => e.MatchId == d.Snapshot.MatchId && e.EventId == d.Snapshot.EventId)).ToArray(),
            ReportContext = new { subjectPlayerId = request.PlayerId, subjectPlayerName = matches.SelectMany(m=>m.Players).FirstOrDefault(p=>p.Id==request.PlayerId)?.Name, scope = request.PlayerId is null ? $"Selected {teamId} players" : "Individual player", matchCount = matches.Length, stats, segments = PerformanceReport.Segments(events),
                matches = matches.Select(m => new { id = m.Id, scenario = m.ScenarioLabel, opponent = m.Teams.First(t => t.Id != teamId).Name, result = m.Result, stats = PerformanceReport.Stats(Events([m], request.PlayerId, teamId)), segments = PerformanceReport.Segments(Events([m], request.PlayerId, teamId)) }) }
        };
        if (provider is DemoInsightProvider) return StatusCode(503,new { error="AI not available here." });
        try { return Ok(await provider.GenerateAsync(input, cancellationToken)); }
        catch (Exception exception) when (exception is InsightProviderException or HttpRequestException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger?.LogWarning("AI report unavailable: {Reason}",exception.Message);
            if(exception is InsightProviderException {RetryAfterSeconds: { } seconds}) Response.Headers.RetryAfter=seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return StatusCode(503,new { error="AI not available here." });
        }
    }
}
public sealed class ReportInsightRequest
{
    public string TeamId { get; set; } = "leeds";
    public string? PlayerId { get; set; }
    public string? MatchId { get; set; }
    public Audience Audience { get; set; } = Audience.Coach;
}
