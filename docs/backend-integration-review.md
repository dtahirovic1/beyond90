# Backend integration review — 24 October 2026

Reviewed Amir commit e6663d3723ede3b42ff6aee5f352f14c7de1c019. Its four files are preserved unchanged during integration.

Useful foundation: typed player/ball positions, available decision options, selected option, body part and a dominant-foot enum. The three example scenes are consistent with the old event sample's player, minute, location and outcome.

Required before enabling spatial analysis:

1. JSON uses `decisionSnapshot` for the pre-action state, while `DecisionSnapshot.cs` calls its constructor property `PreDecision` (web JSON expects `preDecision`). Without an explicit mapping, the model does not receive that object.
2. `AttackDirection(string Arsenal, string Chelsea)` and `MatchScore(int Arsenal, int Chelsea)` plus the schema hardcode two clubs. Use team-ID dictionaries or explicit home/away fields for Leeds and all opponents.
3. A scene has no `eventId`. Add an explicit reference to the existing event; minute alone is not an unambiguous link to one action. Validate match/player/receiver references and `selectedOptionId` membership.
4. MatchStore's deserialized fixture class has no snapshot property and the Foundry mapper sends only name, team, action, outcome and pressure. Nothing in the new scene reaches the running API or GPT yet.
5. The runtime now loads `data/synthetic/leeds/*.json`; Amir's scenes are in the historical `match-demo.json`. The schema now requires decisionSnapshots but the Leeds samples have none. The old schema also has its original fixed dataStatus and no scenarioLabel property. Define a shared multi-club schema before treating those samples as schema-valid.
6. `DominantFoot` is added only to C#: there are no player values in the fixtures and the schema's player object does not allow the property. Add aligned schema/type/value handling. String enum JSON requires JsonStringEnumConverter in fixture loading when these values are populated.
7. `LaneCategory` has no calculation or use yet. Listed pass/shot/dribble options do not establish their feasibility or probabilities. Distances, passing-lane checks and uncertainty handling still need implementation and validation.

The push of the Leeds UI includes its existing catalogue/report API dependencies. It does not claim that the new snapshot analysis is connected or evaluated. No live paid Azure requests are needed for these compatibility checks.
