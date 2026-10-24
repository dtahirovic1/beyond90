namespace Beyond90.Domain;
public static class DecisionSnapshotValidator
{
    public static void Validate(string matchId,IReadOnlyList<MatchTeam> teams,IReadOnlyList<MatchPlayer> players,IReadOnlyList<MatchEvent> events,IReadOnlyList<DecisionSnapshot> snapshots)
    {
        void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition,string reason) { if(!condition) throw new InvalidDataException($"Invalid decision snapshot in {matchId}: {reason}"); }
        bool Coordinate(int x,int y) => x>=0 && x<=100 && y>=0 && y<=100;
        Require(snapshots is not null,"missing snapshots collection");
        var ids=new HashSet<string>(); var eventIds=new HashSet<string>();
        foreach(var s in snapshots!)
        {
            Require(s is not null && !string.IsNullOrWhiteSpace(s.SceneId),"missing scene identity");
            Require(ids.Add(s!.SceneId) && eventIds.Add(s.EventId),"duplicate scene/event");
            var e=events.FirstOrDefault(e=>e.EventId==s.EventId);
            Require(s.MatchId==matchId && e is not null,"event not in this match");
            Require(s.MatchContext is not null && s.Pitch is not null && s.PreDecision?.Ball is not null && s.PreDecision.Players is not null && s.Decision?.Options is not null,"missing context, pitch, preDecision or options");
            Require(s.MatchContext!.Minute==e!.Minute && s.MatchContext.Second==e.Second,"timestamp differs from event");
            Require(s.Decision!.PlayerId==e.PlayerId && string.Equals(s.RecordedOutcome,e.Outcome,StringComparison.OrdinalIgnoreCase),"actor or outcome differs from event");
            var pitch=s.Pitch!;
            Require(pitch.CoordinateUnits=="normalized_0_100" && pitch.LengthMeters>0 && pitch.WidthMeters>0,"invalid pitch units/dimensions");
            Require(pitch.AttackDirection is not null && pitch.AttackDirection.Count==teams.Count && teams.All(t=>pitch.AttackDirection.TryGetValue(t.Id,out var d) && d is "left_to_right" or "right_to_left") && pitch.AttackDirection.Values.Distinct().Count()==2,"invalid team directions");
            Require(s.MatchContext.Score is null || (s.MatchContext.Score.Count==teams.Count && teams.All(t=>s.MatchContext.Score.TryGetValue(t.Id,out var score) && score>=0)),"invalid score");
            var state=s.PreDecision!;
            Require(state.SecondsBeforeAction==0 && Coordinate(state.Ball.X,state.Ball.Y),"only action-time snapshots are supported");
            Require(state.Players.All(p=>p is not null && Coordinate(p.X,p.Y) && players.Any(known=>known.Id==p.Id && known.TeamId==p.TeamId)) && state.Players.Select(p=>p.Id).Distinct().Count()==state.Players.Count,"invalid/duplicate player positions");
            var actor=state.Players.FirstOrDefault(p=>p.Id==e.PlayerId);
            Require(actor is not null && actor.X==e.X && actor.Y==e.Y,"actor location differs from event");
            Require(s.Decision.BodyPartUsed is null or "left_foot" or "right_foot" or "head" or "other","unknown body part");
            var options=s.Decision.Options!;
            Require(options.Count>0 && options.All(o=>o is not null && !string.IsNullOrWhiteSpace(o.Id) && o.Type is "pass" or "shot" or "dribble" or "tackle") && options.Select(o=>o.Id).Distinct().Count()==options.Count,"invalid options");
            var selected=options.FirstOrDefault(o=>o.Id==s.Decision.SelectedOptionId);
            Require(selected is not null && string.Equals(selected.Type,e.Type,StringComparison.OrdinalIgnoreCase),"selected option differs from event action");
            foreach(var option in options)
                Require(option.Type=="pass" ? state.Players.Any(p=>p.Id==option.ReceiverId && p.Id!=actor!.Id && p.TeamId==actor.TeamId) : option.ReceiverId is null,"invalid pass receiver");
            if(selected!.Type is "pass" or "shot" or "dribble")
                Require(state.Ball.X==actor!.X && state.Ball.Y==actor.Y,"ball is not at actor location");
            if(selected.Type=="tackle") Require(options.All(o=>o.Type=="tackle"),"defensive scene cannot assume possession");
        }
    }
}
