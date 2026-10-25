# Decision assessment — prototype rules

## Flow

Synthetic event + same-time player positions → geometric measurements → benefit/risk comparison → provisional verdict → GPT explanation.

The calculator is deterministic C# code. GPT receives its output, player identities, action outcome and score before the action. It explains evidence and trade-offs; it does not calculate a calibrated probability. An outcome is never an input to the comparative score.

## Geometry and comparison units

Coordinates are converted from 0–100 to metres using the declared pitch dimensions. Team attack direction controls forward progression and the target goal.

- Corridor clearance ≤2 m: blocked; ≤4 m: contested; otherwise open among supplied defenders. No supplied opponents means unknown.
- Pass: signed forward progression, pass length, defender distance to the pass segment, and nearest defender to the receiver.
- Shot: distance to goal centre, opening angle between posts, and centreline obstruction. This is not xG and does not know goalkeeper reactions.
- Carry: available space in a forward corridor up to 10 m. It is an illustrative direction, not a recorded movement trajectory.
- Tackles: no comparative geometric model yet; return insufficient context rather than invent a rating.

All values below are **arbitrary comparison units, not percentages**:

| Option | Benefit |
|---|---|
| Pass | clamp(20 + 1.3 × forward metres + min(receiver separation, 12), 0, 80) |
| Shot | clamp(55 − 1.2 × distance + 1.1 × opening angle, 0, 80) |
| Carry | clamp(15 + 2 × available forward metres, 0, 40) |

Base risk is 10 / 30 / 55 for open / contested / blocked corridors. Passes add 0.4 per metre beyond 25 m and 5 per metre of receiver separation below 4 m. Shots add 0.6 per metre beyond 25 m. Risk is capped at 90.

Comparative value = benefit − weight × risk. Weight is 0.9 normally, 1.2 when leading from minute 75, and 0.65 when trailing from minute 75. Unknown score uses neutral weighting. These coefficients need later evaluation against expert-labelled decisions.

At least two assessable options are required. A 15-unit advantage produces “Better visible alternative”; otherwise a chosen risk ≥55 produces “High-risk choice”; otherwise “Reasonable choice”. These labels are conditional on the partial supplied scene. “Insufficient context” is used when comparison cannot be supported.

## Scores without hindsight

Each Leeds fixture declares `goalLogComplete`, a complete **fictional** `goals` ledger and `result`. Recorded action samples remain partial. Loading validates result totals and recorded goals against the ledger. The API computes `scoreBefore` for events and checks/enriches snapshot scores. A goal at the action timestamp is not included until subsequent actions.

Overview reports receive final results. Individual decisions receive only pre-action scores, never final results. One additional late-game scene per match demonstrates leading/trailing context. All scenes and results remain simulated.

## Interface and AI

- Key decisions combines detailed scenes and notable events chronologically; it does not show a numerical impact rating.
- Selecting an option draws its illustrative path. The verdict always describes the actual chosen action, while measurements below describe the selected comparison option.
- Generated decision text is requested as Assessment / Evidence / Alternative / Next action, with a concrete suggestion rather than a reflection question.
- Unconfigured, failed or incomplete AI requests return HTTP 503. The UI shows only "AI not available here." No local review is substituted.

Missing players, movement, offside, goalkeeper information, timing and ball speed remain limitations. Current testing checks consistency and payload wiring; it does not establish football predictive validity or guarantee the quality of every model response.


## Rating and technique update

The UI shows a deterministic, one-decimal model estimate (1–9.5/10), not a professional benchmark or a calibrated probability. It requires two assessable ground options; otherwise the rating is null. Confidence remains Limited for these partial scenes.

Components on a 0–10 scale:
- Risk control = clamp(10 − risk × match-state weight / 10).
- Attacking value = clamp(benefit / 8).
- Option selection = clamp(10 − gap to best supplied option / 8).
- Rating = 35% risk control + 25% attacking value + 40% option selection, rounded to one decimal.
- Strong ≥8, good ≥7, mixed ≥5, poor below 5. The recorded outcome is excluded.

These weights are explicit starting assumptions requiring later expert evaluation. Choosing the least bad option does not automatically receive a high rating because absolute risk and benefit also contribute.

`preDecision.bodySetup`: left_foot / right_foot / head / balanced / null.
`preDecision.ballHeight`: ground / low / high / null.
`decision.options[].bodyPart`: technique needed for each proposed action.

Using the opposite foot to the declared body setup adds 6 risk units; a low ball adds 2. Missing setup/technique data applies no adjustment. Readiness is not dominant foot or proven weak-foot ability. Recorded outcomes are preserved, not rewritten to fit the model. These new technique values describe fictional scenes, not actual player attributes.

High balls/header options are excluded from numeric ground-corridor comparison until a flight/timing model exists. GPT receives this limitation and the technique context. One explicitly aerial Leeds–Liverpool scene illustrates it.

Coach report inputs now separate passing under/without pressure (known outcomes as denominator), shooting, duels and possession losses. The requested narrative is Strengths / Areas to improve / Training priority, scoped to the three tracked players. Decision narratives analyse Assessment / Strength / Trade-off / Coaching point, with the numeric rating displayed separately by the UI.

Verification for this update: API build and TypeScript compilation completed; no test suite or paid model evaluation was run. Existing unavailable-response assertions were updated to the new HTTP 503 contract.


## Analysing a selected option

POST `/api/insights/generate` accepts an optional `optionId` for exactly one event. The server rejects IDs not belonging to that event. Missing optionId analyses the recorded choice.

The calculator evaluates that option against the same pre-action scene without rewriting the snapshot's actual selected option, recorded outcome or technique. `decisionFocus` explicitly carries the evaluated option, hypothetical flag, technique and original played option. A hypothetical alternative has no observed outcome. The response's `decisionReview` is server-generated metadata, including the evaluated option identity and rating; GPT cannot set this rating.

The UI shows the rating only with the generated analysis after pressing Analyse. Changing event, option or role resets the result and aborts its pending request. A failed AI request shows the unavailable message with no stale rating. Position-only and single-event views never invent missing teammate positions.

Player prompts address the selected player as 'you', concentrating on scanning, body setup and personal choices. Coach prompts address the coach, refer to the player by name, and consider evidenced tactical trade-offs and a training cue. Numeric decision quality is role-independent; interpretation differs.


## Report availability and summary-only events

Azure HTTP 429 responses were observed intermittently while other calls returned 200. Performance review requests now send the verified aggregate/per-match report and tracked identities without duplicating all event records and spatial scenes. Event-count evidence remains server-side and in the returned insight.

A 429 starts a deployment-specific server cooldown, honouring Retry-After (60 seconds if absent). Requests during that interval do not call Azure. API responses retain the short unavailable message plus Retry-After; the current UI button waits until retry is allowed. This reduces repeated throttled requests but does not change Azure quota.

Events without a spatial snapshot offer Summarise event. They return a short factual summary and no decisionReview/rating field. They still show the one recorded player location, rather than invent other positions. Detailed scenes retain option analysis and ratings where supported.
