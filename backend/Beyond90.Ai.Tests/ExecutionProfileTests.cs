using Beyond90.Analytics;
using Beyond90.Api;
using Beyond90.Domain;
using System.Text.Json;

namespace Beyond90.Ai.Tests;

public class ExecutionProfileTests
{
    [Fact] public async Task Ai_receives_option_profile_and_qualitative_risk_without_internal_scores()
    {
        var match = MatchStore.LoadCatalog(Path.Combine(AppContext.BaseDirectory, "leeds"));
        var decision = match.Decisions.First();
        var selectedEvent = match.Events.Single(e => e.EventId == decision.Snapshot.EventId);
        string? sent = null;
        using var client = new HttpClient(new FoundryInsightProviderTests.Handler(async (message, token) => {
            sent = await message.Content!.ReadAsStringAsync(token);
            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent(FoundryInsightProviderTests.Envelope("{\"text\":\"A bounded explanation.\"}"))
            };
        }));
        var provider = new FoundryInsightProvider(client, "https://example.services.ai.azure.com/openai/v1/responses", "test", "profiles-test");
        await provider.GenerateAsync(new InsightRequest(match.Id, [selectedEvent.EventId], Audience.Player,
            new Dictionary<string,decimal?>(), [selectedEvent]) { Players = match.Players, Teams = match.Teams, Decisions = [decision] }, default);
        using var envelope = JsonDocument.Parse(sent!);
        using var payload = JsonDocument.Parse(envelope.RootElement.GetProperty("input").GetString()!);
        var option = payload.RootElement.GetProperty("decisionContexts")[0].GetProperty("options")[0];
        Assert.True(option.TryGetProperty("executionProfile", out var profile));
        Assert.NotEmpty(profile.GetProperty("attributes").EnumerateArray());
        Assert.True(option.TryGetProperty("riskLevel", out _));
        Assert.False(option.TryGetProperty("risk", out _));
        Assert.False(payload.RootElement.GetProperty("decisionContexts")[0].GetProperty("verdict").TryGetProperty("rating", out _));
    }
    [Fact] public void Profiles_select_relevant_valid_attributes_and_the_evaluated_foot()
    {
        var player = new MatchPlayer { Passing = 0, Vision = 101, Finishing = 94, Shooting = 90,
            Composure = 88, DominantFoot = DominantFoot.Left, WeakFoot = 3 };
        var pass = ExecutionProfiles.Create(player, new("pass", "pass", "receiver", "right_foot"));
        Assert.Equal(0, pass.Attributes.Single(a => a.Key == "passing").Value);
        Assert.Null(pass.Attributes.Single(a => a.Key == "vision").Value);
        Assert.DoesNotContain(pass.Attributes, a => a.Key == "finishing");
        Assert.Contains("Non-dominant", pass.FootContext);
        Assert.Contains("3/5", pass.FootContext);
        var shot = ExecutionProfiles.Create(player, new("shot", "shot", BodyPart: "left_foot"));
        Assert.Equal(94, shot.Attributes.Single(a => a.Key == "finishing").Value);
        Assert.Contains("Dominant", shot.FootContext);
        Assert.DoesNotContain("3/5", shot.FootContext);
    }

    [Fact] public void Missing_profile_and_aerial_technique_do_not_invent_ability()
    {
        Assert.All(ExecutionProfiles.Create(null, new("carry", "dribble")).Attributes, a => Assert.Null(a.Value));
        var aerial = ExecutionProfiles.Create(new() { Finishing = 99 }, new("head", "shot", BodyPart: "head"));
        Assert.Empty(aerial.Attributes);
        Assert.Contains("heading", aerial.Note);
    }

    [Fact] public void Enrichment_does_not_change_spatial_comparison_or_expose_numeric_grades()
    {
        var match = MatchStore.LoadCatalog(Path.Combine(AppContext.BaseDirectory, "leeds"));
        var plain = DecisionOptionCalculator.Calculate(match.DecisionSnapshots.First());
        var enriched = ExecutionProfiles.Attach(plain, match.Players);
        Assert.Equal(plain.Verdict, enriched.Verdict);
        Assert.Equal(plain.Options.Select(o => o.ComparativeValue), enriched.Options.Select(o => o.ComparativeValue));
        Assert.All(enriched.Options, o => Assert.NotNull(o.ExecutionProfile));
        var json = JsonSerializer.Serialize(enriched, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.DoesNotContain("\"rating\":", json);
        Assert.DoesNotContain("\"comparativeValue\":", json);
    }
}
