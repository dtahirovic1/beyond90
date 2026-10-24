using Beyond90.Ai;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;
namespace Beyond90.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(MatchStore catalogue, IInsightProvider provider, DemoInsightProvider demo) : ControllerBase
{
    [HttpGet("club")]
    public IActionResult Club() => Ok(new { name = "Leeds United", players = catalogue.All.SelectMany(m => m.Players).Where(p => p.TeamId == "leeds").DistinctBy(p => p.Id) });

    private MatchStore[]? Select(string? playerId, string? matchId)
    {
        if (playerId is not null && !catalogue.All.SelectMany(m => m.Players).Any(p => p.TeamId == "leeds" && p.Id == playerId)) return null;
        var matches = catalogue.All.Where(m => matchId is null || m.Id == matchId).ToArray();
        return matches.Length == 0 ? null : matches;
    }
    private static MatchEvent[] Events(IEnumerable<MatchStore> matches, string? playerId) => matches.SelectMany(m => m.Events).Where(e => e.TeamId == "leeds" && (playerId is null || e.PlayerId == playerId)).ToArray();

    [HttpGet]
    public IActionResult Get([FromQuery] string? playerId, [FromQuery] string? matchId)
    {
        var matches = Select(playerId, matchId);
        if (matches is null) return NotFound();
        var stats = PerformanceReport.Stats(Events(matches, playerId));
        return Ok(new { playerId, matchId, matchCount = matches.Length, stats, observations = PerformanceReport.Observations(stats),
            coverage = "Synthetic event samples from selected players. Not complete match statistics or real player performance.",
            matches = matches.Select(m => new { id = m.Id, scenarioLabel = m.ScenarioLabel, opponent = m.Teams.First(t => t.Id != "leeds").Name, stats = PerformanceReport.Stats(Events([m], playerId)) }) });
    }

    [HttpPost("insight")]
    public async Task<IActionResult> Insight([FromBody] ReportInsightRequest request, CancellationToken cancellationToken)
    {
        if (request.Audience is not (Audience.Player or Audience.Coach)) return BadRequest(new { error = "Select Player or Coach." });
        if (request.Audience == Audience.Player && string.IsNullOrWhiteSpace(request.PlayerId)) return BadRequest(new { error = "Select a player." });
        var matches = Select(request.PlayerId, request.MatchId);
        if (matches is null) return NotFound();
        var events = Events(matches, request.PlayerId);
        if (events.Length == 0) return BadRequest(new { error = "No events available for this selection." });
        var stats = PerformanceReport.Stats(events);
        var input = new InsightRequest(request.MatchId ?? "leeds-overview", events.Select(e => e.EventId).ToArray(), request.Audience,
            new Dictionary<string, decimal?> { ["events.selected"] = events.Length }, events)
        {
            Players = matches.SelectMany(m => m.Players).Where(p => p.TeamId == "leeds" && (request.PlayerId is null || p.Id == request.PlayerId)).DistinctBy(p => p.Id).ToArray(),
            Teams = matches[0].Teams.Where(t => t.Id == "leeds").ToArray(),
            ReportContext = new { scope = request.PlayerId is null ? "Selected Leeds players" : "Individual player", matchCount = matches.Length, stats,
                matches = matches.Select(m => new { id = m.Id, scenario = m.ScenarioLabel, opponent = m.Teams.First(t => t.Id != "leeds").Name, stats = PerformanceReport.Stats(Events([m], request.PlayerId)) }) }
        };
        try { return Ok(await provider.GenerateAsync(input, cancellationToken)); }
        catch (Exception exception) when (exception is InsightProviderException or HttpRequestException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            var fallback = await demo.GenerateAsync(input, cancellationToken);
            fallback.Source = "DemoFallback";
            fallback.Caveats.Add("Azure AI is unavailable. Showing a local summary.");
            return Ok(fallback);
        }
    }
}
public sealed class ReportInsightRequest
{
    public string? PlayerId { get; set; }
    public string? MatchId { get; set; }
    public Audience Audience { get; set; } = Audience.Coach;
}
