using System.Text.Json;
using Beyond90.Ai;
using Beyond90.Api;
using Beyond90.Api.Controllers;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Beyond90.Ai.Tests;

public sealed class LeedsReportTests
{
    private static MatchStore Catalogue() => MatchStore.LoadCatalog(Path.Combine(AppContext.BaseDirectory, "leeds"));
    private static JsonElement Json(IActionResult response) => JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(response).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public void Three_scenarios_have_unique_events_and_valid_player_team_links()
    {
        var store = Catalogue();
        Assert.Equal(3, store.All.Count);
        var events = store.All.SelectMany(m => m.Events).ToArray();
        Assert.Equal(events.Length, events.Select(e => e.EventId).Distinct().Count());
        foreach (var match in store.All)
        {
            Assert.Equal(84, match.Events.Count);
            Assert.All(match.Events, e => Assert.Contains(match.Players, p => p.Id == e.PlayerId && p.TeamId == e.TeamId));
        }
    }

    [Theory]
    [InlineData("lee-5")]
    [InlineData("lee-4")]
    [InlineData("lee-9")]
    public void Player_overview_sums_only_that_players_events_across_matches(string playerId)
    {
        var store = Catalogue();
        var controller = new ReportsController(store, new DemoInsightProvider(), new DemoInsightProvider());
        var report = Json(controller.Get(playerId, null));
        Assert.Equal(3, report.GetProperty("matchCount").GetInt32());
        var stats = report.GetProperty("stats");
        Assert.Equal(42, stats.GetProperty("events").GetInt32());
        Assert.Equal(report.GetProperty("matches").EnumerateArray().Sum(m => m.GetProperty("stats").GetProperty("completedPasses").GetInt32()), stats.GetProperty("completedPasses").GetInt32());
        var expected = PerformanceReport.Stats(store.All.SelectMany(m => m.Events).Where(e => e.PlayerId == playerId).ToArray());
        Assert.Equal(expected.Passes, stats.GetProperty("passes").GetInt32());
        Assert.Equal(expected.PassAccuracy, stats.GetProperty("passAccuracy").GetDecimal());
        var single = Json(controller.Get(playerId, "leeds-sim-002"));
        Assert.Equal(14, single.GetProperty("stats").GetProperty("events").GetInt32());
        Assert.Equal(1, single.GetProperty("matchCount").GetInt32());
    }

    [Fact]
    public void Coach_report_excludes_opponent_events_and_unknown_selections_are_rejected()
    {
        var controller = new ReportsController(Catalogue(), new DemoInsightProvider(), new DemoInsightProvider());
        Assert.Equal(126, Json(controller.Get(null, null)).GetProperty("stats").GetProperty("events").GetInt32());
        Assert.IsType<NotFoundResult>(controller.Get("ars-7", null));
        Assert.IsType<NotFoundResult>(controller.Get(null, "missing"));
    }

    [Fact]
    public async Task Insight_selection_cannot_leak_events_from_another_match_and_fan_is_rejected()
    {
        var controller = new InsightsController(Catalogue(), new DemoInsightProvider(), new DemoInsightProvider(), NullLogger<InsightsController>.Instance);
        Assert.IsType<BadRequestObjectResult>(await controller.Generate(new() { MatchId = "leeds-sim-002", EventIds = ["leeds-sim-001-evt-001"], Audience = Audience.Player }, default));
        Assert.IsType<BadRequestObjectResult>(await controller.Generate(new() { MatchId = "leeds-sim-001", EventIds = ["leeds-sim-001-evt-001"], Audience = (Audience)2 }, default));
        Assert.IsType<OkObjectResult>(await controller.Generate(new() { MatchId = "leeds-sim-002", EventIds = ["leeds-sim-002-evt-001"], Audience = Audience.Player }, default));
    }

    [Fact]
    public async Task Player_ai_report_receives_player_only_events_and_aggregate_context()
    {
        var capture = new CaptureProvider();
        var controller = new ReportsController(Catalogue(), capture, new DemoInsightProvider());
        Assert.IsType<OkObjectResult>(await controller.Insight(new() { PlayerId = "lee-5", Audience = Audience.Player }, default));
        Assert.NotNull(capture.Request);
        Assert.All(capture.Request.SelectedEvents, e => Assert.Equal("lee-5", e.PlayerId));
        Assert.Equal(42, capture.Request.SelectedEvents.Count);
        Assert.NotNull(capture.Request.ReportContext);
        Assert.Single(capture.Request.Players);
        Assert.IsType<BadRequestObjectResult>(await controller.Insight(new() { Audience = Audience.Player }, default));
    }

    private sealed class CaptureProvider : IInsightProvider
    {
        public InsightRequest? Request;
        public Task<Insight> GenerateAsync(InsightRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return new DemoInsightProvider().GenerateAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task Azure_report_payload_contains_verified_aggregate_and_per_match_metrics()
    {
        string? payload = null;
        using var client = new HttpClient(new FoundryInsightProviderTests.Handler(async (message, token) =>
        {
            payload = await message.Content!.ReadAsStringAsync(token);
            return FoundryInsightProviderTests.JsonResponse(FoundryInsightProviderTests.Envelope("""{"text":"Review the recorded sample."}"""));
        }));
        var provider = new FoundryInsightProvider(client, "https://example.services.ai.azure.com/openai/v1/responses", "test", "test");
        var controller = new ReportsController(Catalogue(), provider, new DemoInsightProvider());
        Assert.IsType<OkObjectResult>(await controller.Insight(new() { PlayerId = "lee-9", Audience = Audience.Player }, default));
        using var body = JsonDocument.Parse(payload!);
        using var input = JsonDocument.Parse(body.RootElement.GetProperty("input").GetString()!);
        var report = input.RootElement.GetProperty("report");
        Assert.Equal(3, report.GetProperty("matchCount").GetInt32());
        Assert.Equal(42, report.GetProperty("stats").GetProperty("events").GetInt32());
        Assert.Equal(3, report.GetProperty("matches").GetArrayLength());
        Assert.All(input.RootElement.GetProperty("selectedEvents").EnumerateArray(), e => Assert.Equal("Dominic Calvert-Lewin", e.GetProperty("playerName").GetString()));
    }
}
