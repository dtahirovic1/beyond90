using Beyond90.Ai;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Beyond90.Api.Controllers;

[ApiController]
[Route("api/insights")]
public sealed class InsightsController(MatchStore store, IInsightProvider provider, DemoInsightProvider demo,
    ILogger<InsightsController> logger) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateInsightRequest request, CancellationToken cancellationToken)
    {
        if (request.MatchId != store.Id)
            return NotFound(new { error = "Match not found." });
        if (request.EventIds is null || request.EventIds.Length == 0 || request.EventIds.Any(string.IsNullOrWhiteSpace))
            return BadRequest(new { error = "Select at least one event." });
        if (!Enum.IsDefined(request.Audience))
            return BadRequest(new { error = "Audience must be Player, Coach, or Fan." });

        var selectedIds = request.EventIds.Distinct(StringComparer.Ordinal).ToArray();
        var eventsById = store.Events.ToDictionary(matchEvent => matchEvent.EventId, StringComparer.Ordinal);
        if (selectedIds.Any(eventId => !eventsById.ContainsKey(eventId)))
            return BadRequest(new { error = "One or more selected event IDs are unknown." });

        var selectedEvents = selectedIds.Select(eventId => eventsById[eventId]).ToArray();
        var metrics = new Dictionary<string, decimal?>(StringComparer.Ordinal)
        {
            ["events.selected"] = selectedEvents.Length,
            ["passes.attempted"] = store.Metrics.AttemptedPasses,
            ["passes.completed"] = store.Metrics.CompletedPasses,
            ["passes.completionPercentage"] = store.Metrics.PassCompletionPercentage,
            ["pressure.events"] = store.Metrics.PressureEventCount
        };
        foreach (var zone in store.Metrics.TurnoversByZone)
            metrics[$"turnovers.{zone.Key.ToString().ToLowerInvariant()}"] = zone.Value;

        var providerRequest = new InsightRequest(store.Id, selectedIds, request.Audience, metrics, selectedEvents);
        Insight insight;
        try
        {
            insight = await provider.GenerateAsync(providerRequest, cancellationToken);
        }
        catch (Exception exception) when (exception is InsightProviderException or HttpRequestException ||
            (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogWarning("Insight generation unavailable ({FailureType}); returning a labeled demo response.", exception.GetType().Name);
            insight = await demo.GenerateAsync(providerRequest, cancellationToken);
            insight.Source = "DemoFallback";
            insight.Caveats.Add("Azure AI was unavailable or returned an incomplete response. This explanation is a local demo fallback.");
        }

        var validation = EvidenceValidator.Validate(
            insight,
            selectedIds.ToHashSet(StringComparer.Ordinal),
            metrics.Keys.ToHashSet(StringComparer.Ordinal));
        if (insight.Audience != request.Audience)
            return UnprocessableEntity(new { errors = new[] { "Insight audience does not match the requested audience." } });
        if (insight.Caveats is null || insight.MetricEvidence is null || insight.MetricEvidence.Count == 0 ||
            insight.MetricEvidence.Any(evidence => evidence is null || evidence.MetricKey is null ||
                !metrics.TryGetValue(evidence.MetricKey, out var value) || value != evidence.Value))
            return UnprocessableEntity(new { errors = new[] { "Insight evidence does not match the verified metrics." } });
        return validation.IsValid ? Ok(insight) : UnprocessableEntity(new { errors = validation.Errors });
    }
}

public sealed class GenerateInsightRequest
{
    public string MatchId { get; set; } = string.Empty;
    public string[] EventIds { get; set; } = [];
    public Audience Audience { get; set; } = Audience.Fan;
}
