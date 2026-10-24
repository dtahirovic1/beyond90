using Beyond90.Analytics;
using Microsoft.AspNetCore.Mvc;
namespace Beyond90.Api.Controllers;
[ApiController]
[Route("api/matches")]
public sealed class MatchesController(MatchStore catalogue) : ControllerBase
{
    [HttpGet] public IActionResult List() => Ok(catalogue.All.Select(match => match.Summary));
    [HttpGet("{id}")] public IActionResult Get(string id) => catalogue.Find(id) is { } match ? Ok(match.Details) : NotFound();
    [HttpGet("{id}/events")] public IActionResult Events(string id) => catalogue.Find(id) is { } match ? Ok(match.Events) : NotFound();
    [HttpGet("{id}/decisions")] public IActionResult Decisions(string id) => catalogue.Find(id) is { } match ? Ok(match.Decisions) : NotFound();
    [HttpGet("{id}/metrics")] public IActionResult Metrics(string id) => catalogue.Find(id) is { } match ? Ok(match.Metrics) : NotFound();
    [HttpGet("{id}/recap")]
    public IActionResult Recap(string id, [FromQuery] string? playerId = null)
    {
        var match = catalogue.Find(id);
        if (match is null) return NotFound();
        if (playerId is not null && !match.Players.Any(p => p.Id == playerId)) return NotFound();
        var events = match.Events.Where(e => playerId is null || e.PlayerId == playerId).ToArray();
        var byId = events.ToDictionary(e => e.EventId);
        return Ok(new { matchId = id, keyMoments = KeyMomentSelector.Select(events, MetricsCalculator.Calculate(events), 8).Select(moment => new {
            moment.EventIds, moment.Score, moment.ReasonCode, moment.MetricEvidence,
            events = moment.EventIds.Where(byId.ContainsKey).Select(eventId => byId[eventId])
        }) });
    }
}
