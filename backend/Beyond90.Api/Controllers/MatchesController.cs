using Beyond90.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace Beyond90.Api.Controllers;

[ApiController]
[Route("api/matches")]
public sealed class MatchesController(MatchStore store) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(new[] { store.Summary });

    [HttpGet("{id}")]
    public IActionResult Get(string id) => id == store.Id ? Ok(store.Details) : NotFound();

    [HttpGet("{id}/events")]
    public IActionResult Events(string id) => id == store.Id ? Ok(store.Events) : NotFound();

    [HttpGet("{id}/metrics")]
    public IActionResult Metrics(string id)
    {
        if (id != store.Id)
            return NotFound();
        return Ok(store.Metrics);
    }

    [HttpGet("{id}/recap")]
    public IActionResult Recap(string id)
    {
        if (id != store.Id)
            return NotFound();
        var byId = store.Events.ToDictionary(matchEvent => matchEvent.EventId, StringComparer.Ordinal);
        return Ok(new
        {
            matchId = store.Id,
            keyMoments = store.KeyMoments.Select(moment => new
            {
                moment.EventIds,
                moment.Score,
                moment.ReasonCode,
                moment.MetricEvidence,
                events = moment.EventIds.Where(byId.ContainsKey).Select(eventId => byId[eventId])
            })
        });
    }

}
