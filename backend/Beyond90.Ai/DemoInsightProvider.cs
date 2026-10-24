using Beyond90.Domain;
using Beyond90.Analytics;

namespace Beyond90.Ai;

public sealed class DemoInsightProvider : IInsightProvider
{
    public Task<Insight> GenerateAsync(InsightRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var missingOutcome = request.SelectedEvents.Any(matchEvent => string.IsNullOrWhiteSpace(matchEvent.Outcome));
        var events=request.SelectedEvents;
        var names=string.Join(", ",request.Players.Where(p=>events.Any(e=>e.PlayerId==p.Id)).Select(p=>p.Name));
        var metrics=MetricsCalculator.Calculate(events);
        var text=$"Recorded sample — {names}: {events.Count} actions. Passing: {metrics.CompletedPasses} completed out of {metrics.AttemptedPasses} attempted. " +
            $"Shooting: {events.Count(e=>e.Type=="Shot")} shots, {events.Count(e=>e.Type=="Shot" && e.Outcome=="Goal")} goals. " +
            $"Pressure was recorded on {metrics.PressureEventCount} actions. These figures describe execution in the sample, not decision quality.";
        var scenes=request.Decisions.Where(d=>events.Any(e=>e.MatchId==d.Snapshot.MatchId && e.EventId==d.Snapshot.EventId)).ToArray();
        var scene=scenes.FirstOrDefault(d=>d.Verdict?.PreferredOptionId is not null) ?? scenes.FirstOrDefault();
        if(scene?.Verdict is { } verdict)
        {
            var alternative=scene.Options.FirstOrDefault(o=>o.OptionId==verdict.PreferredOptionId);
            var alternativeName=alternative?.Type=="pass" ? "pass to "+request.Players.FirstOrDefault(p=>p.Id==alternative.ReceiverId)?.Name : alternative?.Type;
            var example=events.First(e=>e.EventId==scene.Snapshot.EventId && e.MatchId==scene.Snapshot.MatchId);
            var actor=request.Players.FirstOrDefault(p=>p.Id==example.PlayerId)?.Name??"The selected player";
            var chosen=scene.Options.Single(o=>o.OptionId==scene.Snapshot.Decision.SelectedOptionId);
            var advice=alternative is not null ? $"Consider the {alternativeName}: its {alternative.Corridor.ToLowerInvariant()} corridor and {alternative.ProgressionMeters} m progression offer a stronger visible trade-off. Check unseen opponents before committing." :
                chosen.Risk>=55 ? "With this obstructed corridor, confirm the lane is clear before committing; the scene does not establish a safer available option." :
                verdict.Label=="Insufficient context" ? "A coaching recommendation requires additional opponent positions and alternative actions." :
                "Use the available space, while checking the receiving area and opponents outside this snapshot before committing.";
            text=(request.ReportContext is null ? "" : text+"\n\nExample from the available detailed decisions:\n")+
                $"Assessment — {verdict.Label}. {actor}, {example.Minute}:{example.Second:00}.\n\nEvidence — {verdict.MatchSituation} {verdict.Explanation}\n\n"+
                $"Alternative — {(alternative is null ? "No clearly better supplied option is established." : alternativeName+" offers the stronger provisional comparison.")}\n\n"+
                $"Next action — {advice}\n\nRecorded outcome: {example.Outcome??"unknown"}; this was not used to score the choice.";
        }
        else text+="\n\nAssessment — Insufficient spatial context to compare the chosen action with alternatives. No reliable better-option recommendation can be made from the recorded outcome alone.";
        var caveats = new List<string> { "Synthetic demo insight; it describes illustrative events and does not predict a real match outcome." };
        if (missingOutcome)
            caveats.Add("An outcome is missing for one or more selected events, so those results cannot be assessed.");

        return Task.FromResult(new Insight
        {
            InsightId = $"demo-{Guid.NewGuid():N}",
            Audience = request.Audience,
            EventIds = request.EventIds.ToList(),
            MetricEvidence =
            [
                new MetricEvidence
                {
                    MetricKey = "events.selected",
                    Value = request.SelectedEvents.Count,
                    EventIds = request.EventIds.ToList()
                }
            ],
            Text = text,
            Caveats = caveats
        });
    }
}
