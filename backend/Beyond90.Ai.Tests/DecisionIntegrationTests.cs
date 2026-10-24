using System.Text.Json;
using System.Text.Json.Nodes;
using Beyond90.Api;
using Beyond90.Api.Controllers;
using Beyond90.Ai;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
namespace Beyond90.Ai.Tests;
public class DecisionIntegrationTests
{
    private static string Fixture => Path.Combine(AppContext.BaseDirectory,"leeds","leeds-sim-001.json");
    [Fact] public void Catalogue_scenes_are_linked_and_each_Leeds_player_has_context()
    {
        var catalogue=MatchStore.LoadCatalog(Path.GetDirectoryName(Fixture)!);
        foreach(var match in catalogue.All)
        {
            Assert.Equal(3,match.Decisions.Select(d=>d.Snapshot.Decision.PlayerId).Distinct().Count());
            foreach(var d in match.Decisions) Assert.Contains(match.Events,e=>e.EventId==d.Snapshot.EventId && e.MatchId==d.Snapshot.MatchId);
            Assert.IsType<OkObjectResult>(new MatchesController(catalogue).Decisions(match.Id));
        }
    }
    [Fact] public void Supplied_tackle_scenes_include_the_nearby_duel_opponent()
    {
        var catalogue=MatchStore.LoadCatalog(Path.GetDirectoryName(Fixture)!);
        foreach(var s in catalogue.All.SelectMany(m=>m.DecisionSnapshots).Where(s=>s.Decision.Options.Any(o=>o.Type=="tackle")))
        {
            var actor=s.PreDecision.Players.Single(p=>p.Id==s.Decision.PlayerId);
            Assert.Contains(s.PreDecision.Players,p=>p.TeamId!=actor.TeamId && Math.Sqrt(Math.Pow((p.X-actor.X)*1.05,2)+Math.Pow((p.Y-actor.Y)*.68,2))<3);
        }
    }
    [Theory] [InlineData("event")] [InlineData("receiver")] [InlineData("coordinates")] [InlineData("missingState")]
    public void Invalid_spatial_context_is_rejected_at_load(string mutation)
    {
        var json=JsonNode.Parse(File.ReadAllText(Fixture))!;
        var scene=json["decisionSnapshots"]![0]!;
        if(mutation=="event") scene["eventId"]="another-match-event";
        if(mutation=="receiver") scene["decision"]!["options"]![0]!["receiverId"]="ars-7";
        if(mutation=="coordinates") scene["preDecision"]!["players"]![0]!["x"]=101;
        if(mutation=="missingState") scene.AsObject().Remove("preDecision");
        var path=Path.GetTempFileName();
        try { File.WriteAllText(path,json.ToJsonString()); Assert.Throws<InvalidDataException>(()=>MatchStore.Load(path)); }
        finally { File.Delete(path); }
    }
    [Theory] [InlineData("leeds-sim-001-evt-001",1)] [InlineData("leeds-sim-001-evt-007",0)]
    public async Task GPT_gets_only_selected_event_scene_and_explicit_coverage(string eventId,int count)
    {
        string? input=null;
        using var client=new HttpClient(new FoundryInsightProviderTests.Handler(async (message,token)=>{
            using var body=JsonDocument.Parse(await message.Content!.ReadAsStringAsync(token));
            input=body.RootElement.GetProperty("input").GetString();
            return FoundryInsightProviderTests.JsonResponse(FoundryInsightProviderTests.Envelope("{\"text\":\"Review the visible options.\"}"));
        }));
        var controller=new InsightsController(MatchStore.Load(Fixture),new FoundryInsightProvider(client,"https://example.com/openai/v1/responses","test","test"),new DemoInsightProvider(),NullLogger<InsightsController>.Instance);
        Assert.IsType<OkObjectResult>(await controller.Generate(new(){MatchId="leeds-sim-001",EventIds=[eventId]},default));
        using var doc=JsonDocument.Parse(input!);
        var contexts=doc.RootElement.GetProperty("decisionContexts");
        Assert.Equal(count,contexts.GetArrayLength());
        Assert.False(doc.RootElement.GetProperty("spatialCoverage").GetProperty("fullFormation").GetBoolean());
        if(count>0) {
            Assert.Equal(eventId,contexts[0].GetProperty("snapshot").GetProperty("eventId").GetString());
            Assert.True(contexts[0].GetProperty("options").GetArrayLength()>0);
        }
    }
}
