using Beyond90.Ai;
using Beyond90.Analytics;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Beyond90.Api.Controllers;

[ApiController]
[Route("api/insights")]
public sealed class InsightsController(MatchStore catalogue, IInsightProvider provider, DemoInsightProvider demo,
    ILogger<InsightsController> logger) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateInsightRequest request, CancellationToken cancellationToken)
    {
        var store = catalogue.Find(request.MatchId);
        if (store is null)
            return NotFound(new { error = "Match not found." });
        if (request.EventIds is null || request.EventIds.Length == 0 || request.EventIds.Any(string.IsNullOrWhiteSpace))
            return BadRequest(new { error = "Select at least one event." });
        if (request.Audience is not (Audience.Player or Audience.Coach))
            return BadRequest(new { error = "Audience must be Player or Coach." });

        var selectedIds = request.EventIds.Distinct(StringComparer.Ordinal).ToArray();
        var eventsById = store.Events.ToDictionary(matchEvent => matchEvent.EventId, StringComparer.Ordinal);
        if (selectedIds.Any(eventId => !eventsById.ContainsKey(eventId)))
            return BadRequest(new { error = "One or more selected event IDs are unknown." });

        var selectedEvents = selectedIds.Select(eventId => eventsById[eventId]).ToArray();
        var snapshot=selectedIds.Length==1 ? store.DecisionSnapshots.FirstOrDefault(s=>s.EventId==selectedIds[0]) : null;
        if(request.OptionId is not null && (snapshot is null || !snapshot.Decision.Options.Any(o=>o.Id==request.OptionId)))
            return BadRequest(new {error="Select an option belonging to this event."});
        var option=snapshot?.Decision.Options.Single(o=>o.Id==(request.OptionId??snapshot.Decision.SelectedOptionId));
        var assessment=snapshot is not null && option is not null ? ExecutionProfiles.Attach(DecisionOptionCalculator.Calculate(snapshot,option.Id), store.Players) : null;
        var hypothetical=option is not null && option.Id!=snapshot!.Decision.SelectedOptionId;
        var optionName=option?.Type switch {
            "pass" => "Pass to "+store.Players.First(p=>p.Id==option.ReceiverId).Name,
            "dribble" => "Carry forward", "shot" => "Shoot", "tackle" => "Tackle", _ => selectedEvents[0].Type
        };
        var focus=option is null ? null : new { eventId=selectedIds[0], optionId=option.Id, optionName, action=option.Type,
            option.ReceiverId,option.BodyPart, playedOptionId=snapshot!.Decision.SelectedOptionId,
            isHypothetical=hypothetical, outcome=hypothetical ? null : selectedEvents[0].Outcome };
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

        var providerRequest = new InsightRequest(store.Id, selectedIds, request.Audience, metrics, selectedEvents)
        {
            Players = store.Players,
            Teams = store.Teams,
            Decisions = assessment is not null ? [assessment] : store.Decisions.Where(d => selectedIds.Contains(d.Snapshot.EventId)).ToArray(),
            DecisionFocus = focus,
            RecordedEventCount = store.Events.Count,
            PassesWithKnownOutcome = store.Events.Count(e =>
                string.Equals(e.Type, "Pass", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Outcome))
        };
        if (provider is DemoInsightProvider) return StatusCode(503,new { error="AI not available here." });
        Insight insight;
        try
        {
            insight = await provider.GenerateAsync(providerRequest, cancellationToken);
        }
        catch (Exception exception) when (exception is InsightProviderException or HttpRequestException ||
            (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogWarning("Insight generation unavailable ({FailureType}).", exception.GetType().Name);
            if(exception is InsightProviderException {RetryAfterSeconds: { } seconds}) Response.Headers.RetryAfter=seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return StatusCode(503,new { error="AI not available here." });
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
        if(!validation.IsValid) return UnprocessableEntity(new { errors = validation.Errors });
        insight.DecisionReview=assessment is null ? null : new {optionId=option?.Id,optionName,isHypothetical=hypothetical,
            label=assessment.Verdict?.Label, explanation=assessment.Verdict?.Explanation,
            confidence=assessment?.Verdict?.Confidence??"Limited",audience=request.Audience.ToString()};
        return Ok(insight);
    }
}

public sealed class GenerateInsightRequest
{
    public string MatchId { get; set; } = string.Empty;
    public string[] EventIds { get; set; } = [];
    public Audience Audience { get; set; } = Audience.Player;
    public string? OptionId { get; set; }
}
