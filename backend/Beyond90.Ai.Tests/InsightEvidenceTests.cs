using Beyond90.Ai;
using Beyond90.Api;
using Beyond90.Api.Controllers;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Beyond90.Ai.Tests;

public sealed class InsightEvidenceTests
{
    [Fact]
    public async Task Rejects_a_known_metric_with_an_invented_value()
    {
        var controller = Create(new WrongValueProvider());
        var result = await controller.Generate(Request(), CancellationToken.None);
        Assert.IsType<UnprocessableEntityObjectResult>(result);
    }

    [Fact]
    public async Task Timeout_is_a_labeled_fallback_not_a_successful_AI_result()
    {
        var controller = Create(new FailingProvider());
        var result = await controller.Generate(Request(), CancellationToken.None);
        var insight = Assert.IsType<Insight>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("DemoFallback", insight.Source);
        Assert.NotEmpty(insight.Caveats);
    }

    [Fact]
    public async Task Unknown_event_is_rejected_before_calling_the_provider()
    {
        var controller = Create(new MustNotRunProvider());
        var result = await controller.Generate(new() { MatchId = "b90-demo-001", EventIds = ["unknown"] }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static GenerateInsightRequest Request() => new() { MatchId = "b90-demo-001", EventIds = ["evt-001"] };
    private static InsightsController Create(IInsightProvider provider) => new(
        MatchStore.Load(Path.Combine(AppContext.BaseDirectory, "match-demo.json")), provider,
        new DemoInsightProvider(), NullLogger<InsightsController>.Instance);

    private sealed class WrongValueProvider : IInsightProvider
    {
        public Task<Insight> GenerateAsync(InsightRequest request, CancellationToken token) => Task.FromResult(new Insight
        {
            InsightId = "test", Source = "Foundry", Audience = request.Audience, EventIds = ["evt-001"],
            Text = "Synthetic event.", Caveats = ["Synthetic data."],
            MetricEvidence = [new() { MetricKey = "events.selected", Value = 8, EventIds = ["evt-001"] }]
        });
    }

    private sealed class FailingProvider : IInsightProvider
    {
        public Task<Insight> GenerateAsync(InsightRequest request, CancellationToken token) =>
            throw new TaskCanceledException("Test timeout");
    }

    private sealed class MustNotRunProvider : IInsightProvider
    {
        public Task<Insight> GenerateAsync(InsightRequest request, CancellationToken token) =>
            throw new InvalidOperationException("Unknown input reached provider.");
    }
}
