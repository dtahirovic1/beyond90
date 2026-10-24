using Beyond90.Analytics;
using Beyond90.Api;
using Beyond90.Domain;

namespace Beyond90.Ai.Tests;
public class DecisionUpgradeTests
{
    private static MatchStore Catalogue()=>MatchStore.LoadCatalog(Path.Combine(AppContext.BaseDirectory,"leeds"));
    [Fact] public void Goal_is_not_included_in_its_own_pre_action_score()
    {
        foreach(var match in Catalogue().All)
        {
            Assert.NotNull(match.Result);
            var goal=Assert.Single(match.Events.Where(e=>e.Outcome=="Goal"));
            Assert.Equal(0,goal.ScoreBefore![goal.TeamId]);
            Assert.Equal(1,match.Events.Last().ScoreBefore![goal.TeamId]);
            Assert.All(match.Decisions,d=>Assert.NotNull(d.Snapshot.MatchContext.Score));
        }
    }
    [Fact] public void Outcome_cannot_change_pre_action_verdict()
    {
        var scene=Catalogue().Decisions.First().Snapshot;
        var a=DecisionOptionCalculator.Calculate(scene with {RecordedOutcome="Goal"});
        var b=DecisionOptionCalculator.Calculate(scene with {RecordedOutcome="Incomplete"});
        Assert.Equal(a.Verdict,b.Verdict);
        Assert.Equal(a.Options,b.Options);
    }
    [Fact] public void Late_lead_penalizes_risk_more_than_late_deficit()
    {
        var scene=Catalogue().Decisions.First().Snapshot;
        DecisionAnalysis Calculate(int home,int away)=>DecisionOptionCalculator.Calculate(scene with {MatchContext=scene.MatchContext with {Minute=85,Score=new Dictionary<string,int>{{"leeds",home},{"arsenal",away}}}});
        var leading=Calculate(1,0);var trailing=Calculate(0,1);
        foreach(var option in leading.Options.Where(o=>o.ComparativeValue.HasValue))
            Assert.True(option.ComparativeValue<trailing.Options.Single(o=>o.OptionId==option.OptionId).ComparativeValue);
    }
    [Fact] public void No_opponents_means_no_comparative_verdict()
    {
        var scene=Catalogue().Decisions.First().Snapshot;
        var result=DecisionOptionCalculator.Calculate(scene with {PreDecision=scene.PreDecision with {Players=scene.PreDecision.Players.Where(p=>p.TeamId=="leeds").ToArray()}});
        Assert.Equal("Insufficient context",result.Verdict!.Label);
        Assert.Null(result.Verdict.PreferredOptionId);
    }
    [Fact] public void Progression_can_outweigh_a_safer_backward_pass()
    {
        var scene=Catalogue().Decisions.First().Snapshot;
        scene=scene with {
            PreDecision=new(0,new(50,50),[new("a","leeds",50,50),new("b","leeds",30,50),new("c","leeds",75,50),new("d","arsenal",60,55)]),
            Decision=new("a","back",null,[new("back","pass","b"),new("forward","pass","c")])};
        var result=DecisionOptionCalculator.Calculate(scene);
        Assert.Equal("Open",result.Options[0].Corridor);
        Assert.Equal("Contested",result.Options[1].Corridor);
        Assert.Equal("forward",result.Verdict!.PreferredOptionId);
    }
    [Fact] public async Task Local_review_is_a_finished_assessment_and_only_names_selected_actors()
    {
        var match=Catalogue();var events=match.Events.Where(e=>e.PlayerId=="lee-5").ToArray();
        var request=new InsightRequest(match.Id,events.Select(e=>e.EventId).ToArray(),Audience.Player,new Dictionary<string,decimal?>(),events)
            {Players=match.Players,Teams=match.Teams,Decisions=match.Decisions,ReportContext=new {scope="Player"}};
        var result=await new DemoInsightProvider().GenerateAsync(request,default);
        Assert.Contains("Assessment —",result.Text);
        Assert.Contains("Next action —",result.Text);
        Assert.DoesNotContain("Review the passing",result.Text);
        Assert.StartsWith("Recorded sample — Tarik Muharemović:",result.Text);
    }
}
