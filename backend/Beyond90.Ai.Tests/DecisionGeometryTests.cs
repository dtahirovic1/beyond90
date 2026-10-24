using Beyond90.Analytics;
using Beyond90.Domain;
namespace Beyond90.Ai.Tests;
public class DecisionGeometryTests
{
    private static readonly PitchInfo Pitch = new("normalized_0_100",105,68,new Dictionary<string,string>{{"leeds","left_to_right"},{"away","right_to_left"}});
    [Fact] public void Defender_on_middle_of_pass_blocks_corridor_even_when_receiver_is_free()
    {
        var result = DecisionOptionCalculator.Pass("pass", new("a","leeds",10,50), new("b","leeds",90,50), [new("c","away",50,50)], Pitch);
        Assert.Equal(84m,result.DistanceMeters);
        Assert.Equal("Blocked",result.Corridor);
        Assert.Equal(0m,result.CorridorClearanceMeters);
    }
    [Fact] public void Defender_beyond_receiver_does_not_block_segment()
    {
        var result = DecisionOptionCalculator.Pass("pass",new("a","leeds",10,50),new("b","leeds",50,50),[new("c","away",90,50)],Pitch);
        Assert.Equal("Open",result.Corridor);
        Assert.Equal(42m,result.CorridorClearanceMeters);
    }
    [Fact] public void Defender_just_beyond_receiver_still_obstructs_receiving_area()
    {
        var result=DecisionOptionCalculator.Pass("pass",new("a","leeds",10,50),new("b","leeds",50,50),[new("c","away",51,50)],Pitch);
        Assert.Equal("Blocked",result.Corridor);
        Assert.Equal(1.05m,result.CorridorClearanceMeters);
    }
    [Fact] public void Shot_uses_normalized_goal_center_and_team_direction()
    {
        var result = DecisionOptionCalculator.Shot("shot",new("a","leeds",80,50),[],Pitch);
        Assert.Equal(21m,result.DistanceMeters);
        var mirrored = DecisionOptionCalculator.Shot("shot",new("b","away",20,50),[],Pitch);
        Assert.Equal(result.DistanceMeters,mirrored.DistanceMeters);
        Assert.Equal("Unknown",result.Corridor);
        Assert.Null(result.NearestOpponentMeters);
    }
    [Fact] public void Dribble_only_measures_forward_corridor_in_metres()
    {
        var result=DecisionOptionCalculator.Dribble("carry",new("a","leeds",80,50),[new("c","away",70,50)],Pitch);
        Assert.Equal(21m,result.DistanceMeters);
        Assert.Equal(10m,result.SpaceAheadMeters);
        Assert.Equal(0,result.OpponentsInCorridor);
    }
}
