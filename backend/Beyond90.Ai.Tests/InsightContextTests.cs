using System.Text.Json;
using Beyond90.Ai;
using Beyond90.Api;
using Beyond90.Api.Controllers;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Beyond90.Ai.Tests;

public sealed class InsightContextTests
{
    [Theory]
    [InlineData("evt-001", "Martin Ødegaard", "Arsenal", "Completed", false)]
    [InlineData("evt-011", "Moisés Caicedo", "Chelsea", null, null)]
    public async Task Azure_payload_contains_only_selected_event_facts_and_preserves_unknowns(
        string eventId, string playerName, string teamName, string? outcome, bool? pressure)
    {
        string? input = null;
        using var client = new HttpClient(new FoundryInsightProviderTests.Handler(async (message, token) =>
        {
            using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync(token));
            input = body.RootElement.GetProperty("input").GetString();
            return FoundryInsightProviderTests.JsonResponse(FoundryInsightProviderTests.Envelope("""
                {"text":"A recorded football action."}
                """));
        }));
        var provider = new FoundryInsightProvider(client, "https://example.services.ai.azure.com/openai/v1/responses", "test", "test");
        var controller = new InsightsController(MatchStore.Load(Path.Combine(AppContext.BaseDirectory, "match-demo.json")),
            provider, new DemoInsightProvider(), NullLogger<InsightsController>.Instance);
        var response = await controller.Generate(
            new GenerateInsightRequest { MatchId = "b90-demo-001", EventIds = [eventId] }, CancellationToken.None);
        var insight = Assert.IsType<Insight>(Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(eventId, Assert.Single(insight.EventIds));
        Assert.Equal("events.selected", Assert.Single(insight.MetricEvidence).MetricKey);
        Assert.Contains(insight.Caveats, caveat => caveat.Contains("Synthetic", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(outcome is null, insight.Caveats.Any(caveat => caveat.Contains("outcome is not recorded", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(pressure is null, insight.Caveats.Any(caveat => caveat.Contains("Pressure information is not recorded", StringComparison.OrdinalIgnoreCase)));

        using var context = JsonDocument.Parse(input!);
        var root = context.RootElement;
        var selected = root.GetProperty("selectedEvents")[0];
        Assert.Equal(playerName, selected.GetProperty("playerName").GetString());
        Assert.Equal(teamName, selected.GetProperty("teamName").GetString());
        Assert.Equal(outcome, selected.GetProperty("outcome").GetString());
        Assert.Equal(pressure, selected.GetProperty("underPressure").ValueKind == JsonValueKind.Null
            ? (bool?)null : selected.GetProperty("underPressure").GetBoolean());
        Assert.Equal("Player", root.GetProperty("audience").GetString());
        Assert.True(root.GetProperty("isSynthetic").GetBoolean());
        Assert.False(root.TryGetProperty("selectionMetrics", out _));
        Assert.False(root.TryGetProperty("matchTotals", out _));
        Assert.False(root.TryGetProperty("selectedEventIds", out _));
        Assert.Equal(eventId, selected.GetProperty("eventId").GetString());
        Assert.True(selected.TryGetProperty("x", out _));
        Assert.True(selected.TryGetProperty("y", out _));
    }
}
