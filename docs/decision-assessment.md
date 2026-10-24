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
- A local fallback produces a finished evidence-based summary and is visibly labelled local. A recommendation is withheld when context is insufficient.

Missing players, movement, offside, goalkeeper information, timing and ball speed remain limitations. Current testing checks consistency and payload wiring; it does not establish football predictive validity or guarantee the quality of every model response.
