using Beyond90.Domain;

namespace Beyond90.Ai;

public sealed class DemoInsightProvider : IInsightProvider
{
    public Task<Insight> GenerateAsync(InsightRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var eventList = string.Join(", ", request.EventIds);
        var missingOutcome = request.SelectedEvents.Any(matchEvent => string.IsNullOrWhiteSpace(matchEvent.Outcome));
        var voice = request.Audience switch
        {
            Audience.Player => missingOutcome
                ? $"Player view: review the selected action at {eventList}; its outcome is not recorded."
                : $"Player view: review the selected action at {eventList} and compare its recorded outcome with the context.",
            Audience.Coach => $"Coach view: the selected sequence ({eventList}) is available for tactical review.",
            _ => $"Fan view: the replay highlights the selected moment at {eventList}."
        };
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
            Text = $"Demo insight (synthetic). {voice}",
            Caveats = caveats
        });
    }
}
