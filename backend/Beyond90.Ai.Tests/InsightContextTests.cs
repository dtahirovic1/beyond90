using System.Text.Json;
using Beyond90.Ai;
using Beyond90.Api;
using Beyond90.Api.Controllers;
using Beyond90.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace Beyond90.Ai.Tests;

public sealed class InsightContextTests
{
    [Theory]
    [InlineData("evt-001", "Martin Ødegaard", "Arsenal", "Completed", false)]
    [InlineData("evt-011", "Moisés Caicedo", "Chelsea", null, null)]
    public async Task Azure_payload_resolves_identity_separates_totals_and_preserves_unknowns(
        string eventId, string playerName, string teamName, string? outcome, bool? pressure)
    {
        string? input = null;
        using var client = new HttpClient(new FoundryInsightProviderTests.Handler(async (message, token) =>
        {
            using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync(token));
            input = body.RootElement.GetProperty("input").GetString();
            return FoundryInsightProviderTests.JsonResponse(FoundryInsightProviderTests.Envelope("""
                {"audience":"Fan","eventIds":["evt-001"],"metricEvidence":[],"text":"test","caveats":[]}
                """));
        }));
        var provider = new FoundryInsightProvider(client, "https://example.services.ai.azure.com/openai/v1/responses", "test", "test");
        var controller = new InsightsController(MatchStore.Load(Path.Combine(AppContext.BaseDirectory, "match-demo.json")),
            provider, new DemoInsightProvider(), NullLogger<InsightsController>.Instance);
        await controller.Generate(new GenerateInsightRequest { MatchId = "b90-demo-001", EventIds = [eventId] }, CancellationToken.None);

        using var context = JsonDocument.Parse(input!);
        var root = context.RootElement;
        var selected = root.GetProperty("selectedEvents")[0];
        Assert.Equal(playerName, selected.GetProperty("playerName").GetString());
        Assert.Equal(teamName, selected.GetProperty("teamName").GetString());
        Assert.Equal(outcome, selected.GetProperty("outcome").GetString());
        Assert.Equal(pressure, selected.GetProperty("underPressure").ValueKind == JsonValueKind.Null
            ? (bool?)null : selected.GetProperty("underPressure").GetBoolean());
        Assert.Equal(1, root.GetProperty("selectionMetrics").GetProperty("events.selected").GetInt32());
        var totals = root.GetProperty("matchTotals");
        Assert.Equal("both_teams_recorded_events", totals.GetProperty("scope").GetString());
        Assert.Equal(15, totals.GetProperty("recordedEventCount").GetInt32());
        Assert.Equal(7, totals.GetProperty("passesWithKnownOutcome").GetInt32());
        Assert.Equal(8, totals.GetProperty("values").GetProperty("passes.attempted").GetInt32());
        Assert.False(root.TryGetProperty("verifiedMetrics", out _));
    }
}
