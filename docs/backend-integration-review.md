# Decision scene integration

Status: implemented locally, pending user review and push approval.

- Thirteen synthetic, action-time spatial snapshots across three Leeds scenarios. Each tracked player has a scene in each match. The remaining events have no spatial snapshot.
- Every snapshot names its match and event; timestamp, actor, outcome, team directions, options and positions are validated on loading.
- The old JSON field `decisionSnapshot` was renamed to `preDecision`; team score and attack direction maps are generic. Unknown scores remain null.
- Corrected legacy scene with a pass option lacking a receiver. Existing Leeds scenes were linked to compatible actor/actions; when the actor moved, the whole local formation moved with it.
- Defensive tackle scenes do not claim possession or attacking alternatives.
- GET /api/matches/{id}/decisions returns snapshot, computed options and coverage.
- Event insights receive only that event's scene. Player/coach reports include relevant scenes with match/event IDs and explicit partial coverage. Statistics still use recorded events only.
- The pitch and AI share the same validated scene and computed descriptors. Missing scenes show the single recorded action, without invented teammates or ball destinations.

## Geometry
Coordinates use 0..100, converted to metres with the scene's pitch dimensions. Pass clearance is the minimum distance of a visible opponent to the bounded passing segment, including endpoints. Labels use 2 m (Blocked) and 4 m (Contested); larger clearances are Open among visible players only. No supplied opponents means Unknown.

Shots target the correct goal centre using attack direction; output includes distance, goal opening angle (7.32 m goal width) and static centre-line obstruction. This is not xG or a full goal-mouth obstruction model.

Dribbles examine a forward corridor up to 10 m, capped at the goal line. Space ahead stops 2 m before a visible obstacle in that corridor. Distance to the goal line is reported separately. No movement, acceleration, ball speed, offside, goalkeeper reach, fatigue or success probability is inferred.

The scenes show only 5 or 6 players; missing defenders may change every option assessment. Selecting an option shows a proposed path, not measured ball travel. See decision-assessment.md for the benefit/risk comparison and score ledger.

## Previous integration verification
30 AI/integration/geometry tests pass, including endpoint obstruction, mirrored attacks, cross-match references, invalid receivers and selected-scene payload filtering. JSON Schema validation passes for all four fixture files. Frontend TypeScript and production build pass. Azure requests were verified with a fake HTTP handler; no paid model call was made.


## Decision upgrade verification
29 targeted tests passed (decision geometry, verdicts, score timing, reports and request context); TypeScript and production build passed. No live model quality evaluation was performed in this update.
