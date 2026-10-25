using Beyond90.Domain;
namespace Beyond90.Analytics;

// Geometric descriptors of a partial static scene. These are not success probabilities.
public sealed record OptionAssessment(string OptionId, string Type, string? ReceiverId, decimal? DistanceMeters,
    decimal? NearestOpponentMeters, decimal? CorridorClearanceMeters, int OpponentsInCorridor,
    decimal? SpaceAheadMeters, decimal? GoalOpeningDegrees, string Corridor, string Reason)
{
    public decimal? ProgressionMeters { get; init; }
    public decimal? Benefit { get; init; }
    public decimal? Risk { get; init; }
    public decimal? ComparativeValue { get; init; }
    public decimal? SetupRiskAdjustment { get; init; }
    public string TechniqueNote { get; init; } = "Technique context unavailable.";
}
public sealed record RatingComponents(decimal RiskControl, decimal AttackingValue, decimal OptionSelection);
public sealed record DecisionVerdict(string Label, string? PreferredOptionId, string Explanation, string MatchSituation)
{
    public decimal? Rating { get; init; }
    public string RatingLabel { get; init; } = "Not enough data to rate";
    public string Confidence { get; init; } = "Limited";
    public RatingComponents? Components { get; init; }
    public string RatingMethod { get; init; } = "Heuristic v1; not calibrated against expert ratings.";
}
public sealed record DecisionAnalysis(DecisionSnapshot Snapshot, IReadOnlyList<OptionAssessment> Options,
    int VisiblePlayers, int VisibleOpponents, string Coverage)
{
    public DecisionVerdict? Verdict { get; init; }
}

public static class DecisionOptionCalculator
{
    private const double BlockRadius = 2;
    private const double ContestRadius = 4;
    private const double DribbleHorizon = 10;
    private static (double X,double Y) Metres(PlayerPosition p, PitchInfo pitch) => (p.X*pitch.LengthMeters/100d,p.Y*pitch.WidthMeters/100d);
    private static double Distance((double X,double Y) a,(double X,double Y) b) => Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2));
    private static decimal Round(double value) => Math.Round((decimal)value,2);
    private static int Direction(PlayerPosition actor,PitchInfo pitch) => pitch.AttackDirection[actor.TeamId] == "left_to_right" ? 1 : -1;
    private static double? Clearance((double X,double Y) start,(double X,double Y) end,(double X,double Y) opponent, bool includeEndpoints)
    {
        var dx=end.X-start.X; var dy=end.Y-start.Y; var squared=dx*dx+dy*dy;
        if(squared==0) return null;
        var t=((opponent.X-start.X)*dx+(opponent.Y-start.Y)*dy)/squared;
        if(!includeEndpoints && (t<0 || t>1)) return null;
        t=Math.Clamp(t,0,1);
        return Distance(opponent,(start.X+t*dx,start.Y+t*dy));
    }
    private static OptionAssessment Assess(string id,string type,string? receiver,PlayerPosition actor,
        (double X,double Y) target,IReadOnlyList<PlayerPosition> opponents,PitchInfo pitch,double? space=null,double? goalAngle=null)
    {
        var start=Metres(actor,pitch);
        var defenders=opponents.Select(p=>Metres(p,pitch)).ToArray();
        var distances=defenders.Select(p=>Clearance(start,target,p,type!="dribble")).Where(d=>d.HasValue).Select(d=>d!.Value).ToArray();
        double? clearance=distances.Length==0 ? null : distances.Min();
        var lane=defenders.Length==0 ? "Unknown" : clearance <= BlockRadius ? "Blocked" : clearance <= ContestRadius ? "Contested" : "Open";
        var nearPoint=type=="pass" ? target : start;
        decimal? nearest=defenders.Length==0 ? null : Round(defenders.Min(p=>Distance(nearPoint,p)));
        return new(id,type,receiver,Round(Distance(start,target)),nearest,clearance.HasValue ? Round(clearance.Value) : null,
            distances.Count(d=>d<=ContestRadius),space.HasValue ? Round(space.Value) : null,goalAngle.HasValue ? Round(goalAngle.Value) : null,lane,
            lane=="Unknown" ? "No opponent positions supplied; corridor cannot be assessed." : $"{lane} among visible opponents only; static geometric corridor (2 m blocked, 4 m contested). Missing defenders, movement and ball speed may change the assessment.");
    }
    public static OptionAssessment Pass(string id,PlayerPosition passer,PlayerPosition receiver,IReadOnlyList<PlayerPosition> opponents,PitchInfo pitch)
        => Assess(id,"pass",receiver.Id,passer,Metres(receiver,pitch),opponents,pitch);
    public static OptionAssessment Shot(string id,PlayerPosition actor,IReadOnlyList<PlayerPosition> opponents,PitchInfo pitch)
    {
        var goalX=Direction(actor,pitch)>0 ? pitch.LengthMeters : 0d;
        var goalY=pitch.WidthMeters/2d; var start=Metres(actor,pitch);
        var a=(X:goalX-start.X,Y:goalY-3.66-start.Y); var b=(X:goalX-start.X,Y:goalY+3.66-start.Y);
        var angle=Math.Atan2(Math.Abs(a.X*b.Y-a.Y*b.X),a.X*b.X+a.Y*b.Y)*180/Math.PI;
        return Assess(id,"shot",null,actor,(goalX,goalY),opponents,pitch,goalAngle:angle);
    }
    public static OptionAssessment Dribble(string id,PlayerPosition actor,IReadOnlyList<PlayerPosition> opponents,PitchInfo pitch)
    {
        var start=Metres(actor,pitch); var direction=Direction(actor,pitch);
        var toLine=direction>0 ? pitch.LengthMeters-start.X : start.X;
        var horizon=Math.Min(DribbleHorizon,toLine);
        var ahead=opponents.Select(p=>Metres(p,pitch)).Select(p=>(Forward:(p.X-start.X)*direction,Side:Math.Abs(p.Y-start.Y)))
            .Where(p=>p.Forward>=0 && p.Forward<=horizon && p.Side<=BlockRadius).Select(p=>Math.Max(0,p.Forward-BlockRadius)).ToArray();
        var space=ahead.Length==0 ? horizon : ahead.Min();
        var result=Assess(id,"dribble",null,actor,(start.X+direction*horizon,start.Y),opponents,pitch,space:opponents.Count==0 ? null : space);
        return result with { DistanceMeters=Round(toLine), Reason=result.Reason+" Space ahead examines at most 10 m in the attack direction; it is not a predicted dribble route." };
    }
    public static DecisionAnalysis Calculate(DecisionSnapshot snapshot) => Calculate(snapshot, snapshot.Decision.SelectedOptionId);
    public static DecisionAnalysis Calculate(DecisionSnapshot snapshot, string evaluatedOptionId)
    {
        var actor=snapshot.PreDecision.Players.Single(p=>p.Id==snapshot.Decision.PlayerId);
        var opponents=snapshot.PreDecision.Players.Where(p=>p.TeamId!=actor.TeamId).ToArray();
        var options=snapshot.Decision.Options.Select(o=>o.Type switch {
            "pass" => Pass(o.Id,actor,snapshot.PreDecision.Players.Single(p=>p.Id==o.ReceiverId),opponents,snapshot.Pitch),
            "shot" => Shot(o.Id,actor,opponents,snapshot.Pitch),
            "dribble" => Dribble(o.Id,actor,opponents,snapshot.Pitch),
            _ => new OptionAssessment(o.Id,o.Type,o.ReceiverId,null,null,null,0,null,null,"Not assessed","This action has no geometric option model.")
        }).ToArray();
        var score=snapshot.MatchContext.Score;
        int? margin=score is null ? null : score[actor.TeamId]-score.Where(s=>s.Key!=actor.TeamId).Sum(s=>s.Value);
        var late=snapshot.MatchContext.Minute>=75;
        var riskWeight=late && margin>0 ? 1.2m : late && margin<0 ? .65m : .9m;
        var situation=margin is null ? "Score unknown; neutral risk weighting." :
            $"{snapshot.MatchContext.Minute} min; acting team {(margin>0 ? "leading" : margin<0 ? "trailing" : "level")}. " +
            (late && margin>0 ? "Protecting the lead gives risk more weight." : late && margin<0 ? "Chasing the game gives progression more weight." : "Balanced risk weighting.");
        options=options.Select(o=> {
            var technique=snapshot.Decision.Options.Single(x=>x.Id==o.OptionId).BodyPart;
            var setup=snapshot.PreDecision.BodySetup;
            var height=snapshot.PreDecision.BallHeight;
            // Aerial trajectories/contact timing need a different model; do not reuse a ground corridor rating.
            if(height=="high" || technique=="head") return o with {Corridor="Not assessed",TechniqueNote="Aerial ball: contact timing and ball trajectory are needed to rate this option."};
            if(o.Corridor is "Unknown" or "Not assessed") return o;
            var progression=o.Type=="pass" ? (snapshot.PreDecision.Players.Single(p=>p.Id==o.ReceiverId).X-actor.X)*snapshot.Pitch.LengthMeters/100m*Direction(actor,snapshot.Pitch)
                : o.Type=="dribble" ? o.SpaceAheadMeters??0 : 0;
            // Dimensionless comparison units, deliberately not calibrated percentages or xG.
            var benefit=o.Type=="shot" ? Math.Clamp(55-(o.DistanceMeters??55)*1.2m+(o.GoalOpeningDegrees??0)*1.1m,0,80)
                : o.Type=="pass" ? Math.Clamp(20+progression*1.3m+Math.Min(o.NearestOpponentMeters??0,12),0,80)
                : Math.Clamp(15+progression*2,0,40);
            var risk=o.Corridor=="Blocked" ? 55m : o.Corridor=="Contested" ? 30m : 10m;
            if(o.Type=="pass") risk+=Math.Max(0,(o.DistanceMeters??0)-25)*.4m+Math.Max(0,4-(o.NearestOpponentMeters??4))*5;
            if(o.Type=="shot") risk+=Math.Max(0,(o.DistanceMeters??0)-25)*.6m;
            decimal? adjustment=technique is "left_foot" or "right_foot" && setup is "left_foot" or "right_foot" or "balanced" && height is "ground" or "low"
                ? (setup=="balanced" || technique==setup ? 0m : 6m)+(height=="low" ? 2m : 0m) : null;
            var techniqueNote=adjustment is null ? "Body setup or option technique is unknown; no technique adjustment applied." :
                setup!="balanced" && technique!=setup ? "This option uses the opposite foot to the current body setup; repositioning may make execution harder." : "The option technique fits the current body setup.";
            if(height=="low") techniqueNote+=" A bouncing/low ball adds contact difficulty.";
            risk=Math.Min(90,risk+(adjustment??0));
            return o with { ProgressionMeters=Math.Round(progression,2), Benefit=Math.Round(benefit,2), Risk=Math.Round(risk,2), ComparativeValue=Math.Round(benefit-riskWeight*risk,2),SetupRiskAdjustment=adjustment,TechniqueNote=techniqueNote };
        }).ToArray();
        var chosen=options.Single(o=>o.OptionId==evaluatedOptionId);
        var ranked=options.Where(o=>o.ComparativeValue.HasValue).OrderByDescending(o=>o.ComparativeValue).ToArray();
        var sufficient=chosen.ComparativeValue.HasValue && ranked.Length>=2;
        var best=ranked.FirstOrDefault();
        var gap=sufficient ? best!.ComparativeValue!.Value-chosen.ComparativeValue!.Value : 0;
        RatingComponents? components=sufficient ? new(
            Math.Round(Math.Clamp(10-chosen.Risk!.Value*riskWeight/10,0,10),1),
            Math.Round(Math.Clamp(chosen.Benefit!.Value/8,0,10),1),
            Math.Round(Math.Clamp(10-gap/8,0,10),1)) : null;
        decimal? rating=components is null ? null : Math.Round(Math.Clamp(.35m*components.RiskControl+.25m*components.AttackingValue+.4m*components.OptionSelection,1,9.5m),1);
        var label=!sufficient ? "Insufficient context" : gap>=15 ? "Better visible alternative" : chosen.Risk>=55 ? "High-risk choice" : "Reasonable choice";
        var explanation=!sufficient ? "At least two assessable options are needed for a comparative verdict. The recorded outcome alone cannot establish decision quality." :
            $"Evaluated option: {chosen.Corridor.ToLowerInvariant()} corridor" +
            (chosen.Type=="shot" ? $", {chosen.DistanceMeters} m from goal and {chosen.GoalOpeningDegrees}° goal opening." : $", {chosen.ProgressionMeters} m forward progress.") +
            (gap>=15 ? " Another supplied option offers a better balance of progression, space and visible risk." : " No supplied option has a clearly stronger balance of benefit and risk.") +
            " This is a provisional comparison of visible options, not a proven optimal decision.";
        return new(snapshot,options,snapshot.PreDecision.Players.Count,opponents.Length,
            "Synthetic partial snapshot. Unseen players, offside, movement and ball speed remain unknown. Comparison scores are uncalibrated rules, not success probabilities. The recorded outcome does not affect the verdict.")
            { Verdict=new(label,sufficient && gap>=15 ? best!.OptionId : null,explanation,situation) {
                Rating=rating,Components=components,RatingLabel=rating is null ? "Not enough data to rate" : rating>=8 ? "Strong choice" : rating>=7 ? "Good choice" : rating>=5 ? "Mixed choice" : "Poor choice"
            } };
    }
}
