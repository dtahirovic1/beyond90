# Beyond90

**The Game Doesn't End at Full Time.**

**[Open the live demo](https://beyond90-ffjd.onrender.com/)**

Beyond90 is an AI-assisted football analysis web app for players and coaches. Choose **Leeds United** or **Manchester City**, explore player profiles, review match actions, and compare decisions with explanations from Azure AI.

All player ratings, season totals, match events and results in this demo are **synthetic**. Real club and player names identify the demonstration squad; the app does not claim to measure their real performance or analyze match video.

## What the app does

- **Club selection:** Leeds and City each have a matchroom, a pixel-art club emblem and three tracked players with named, numbered pixel jerseys. Opponent shirts include Arsenal, Chelsea, Liverpool and Tottenham.
- **Player view:** individual profiles, attribute charts, season snapshots, aggregate performance and individual match reviews.
- **Coach view:** club reports, tracked-player profiles and comparisons across match scenarios.
- **Attribute analysis:** horizontal skill bars and an eight-axis radar with an optional comparison against the tracked squad average. Exact radar values are available in a table; missing values remain unavailable instead of becoming zero.
- **Match review:** recorded actions, outcomes, pressure, key decisions and partial player-position diagrams.
- **Decision comparison:** inspect a played action and the available passing, shooting, carrying or tackling options where a snapshot supplies them. Qualitative risk, measured progression, defender distances and a pitch highlight explain the visible situation. No numerical decision grades are shown.
- **Execution context:** each option shows relevant synthetic attributes (passing/vision, shooting/finishing/composure, dribbling/ball control/acceleration, or defending/strength) and available foot context. These inform the explanation without changing spatial measurements or predicting success. Heading ability is unavailable rather than inferred from finishing.
- **Azure AI reviews:** generate player or coaching summaries, and analyse the selected played action or hypothetical alternative.
- **Responsive interface:** the same player and match flows work on desktop and mobile.

## Clubs and demonstration players

| Club | Tracked players |
| --- | --- |
| Leeds United | Tarik Muharemović, Ethan Ampadu, Dominic Calvert-Lewin |
| Manchester City | Erling Haaland, Rayan Cherki, Marc Guéhi |

Each club has **four match scenarios**, against Arsenal, Chelsea, Liverpool and Tottenham Hotspur. There are **eight bundled fixtures**, with **84 recorded events per fixture**. Reports aggregate actions from the selected tracked players, not complete statistics for all eleven players.

## Player profiles: 25 fields

| Category | Fields |
| --- | --- |
| Profile | Position, age, dominant foot, weak foot |
| Ratings | Overall rating, potential |
| Physical attributes | Pace, acceleration, sprint speed, strength, stamina |
| Technical and mental attributes | Shooting, finishing, passing, vision, dribbling, ball control, defending, interceptions, composure |
| Season snapshot | Minutes played, goals, assists, expected goals (xG), expected assists (xA) |

Skill bars use the supplied **0–100 ratings**. The radar shows acceleration, sprint speed, strength, stamina, composure, ball control, dribbling and pace. Its comparison is the mean of available values for the three tracked players in the same club, including the selected player; it is not a league percentile.

Weak foot uses a **1–5 scale**. Season totals retain their own units and are separate from the recorded match-event samples. These profiles do not contain measured running distance or sprint counts.

## Try the demo

1. Open the [live app](https://beyond90-ffjd.onrender.com/), then choose Leeds or City.
2. Choose **Player**, select a player and explore the skill bars, radar and season snapshot.
3. Scroll to **Explore a match** and open a scenario.
4. Select a key decision, compare the available options and click **Analyse** for the selected option.
5. Return to the club matchroom and choose **Coach** for a club overview and AI coaching review.

The free hosting service can take about a minute to wake after inactivity. Load the app before starting a recorded walkthrough. The public app URL can be used in Demosmith and hackathon submissions.

## Stack

- **Frontend:** React, TypeScript, Vite, Tailwind CSS and SVG.
- **Backend:** ASP.NET Core Web API on .NET 10.
- **Data:** bundled synthetic JSON fixtures and player profiles; no database is required.
- **AI:** Azure Foundry Responses API, using the configured model deployment (`gpt-4.1-mini` by default).
- **Hosting:** the frontend and API run together in a multi-stage Docker deployment on the same public origin.

## Run locally

Requires **.NET 10 SDK** and **Node.js 22.12+**.

Start the API from the repository root:

```powershell
dotnet run --project backend/Beyond90.Api --launch-profile http
```

In a second terminal:

```powershell
cd frontend
npm ci
npm run dev
```

Open the URL printed by Vite, usually `http://localhost:5173`. The frontend proxies `/api` requests to `http://127.0.0.1:5089`.

Alternatively, open `backend/Beyond90.sln` in Visual Studio, set **Beyond90.Api** as the startup project and run the **http** profile.

### Configure Azure AI

In Visual Studio, right-click **Beyond90.Api → Manage User Secrets** and configure:

```json
{
  "Foundry": {
    "Enabled": true,
    "Endpoint": "https://YOUR-RESOURCE.services.ai.azure.com/openai/v1/responses",
    "Deployment": "YOUR-DEPLOYMENT-NAME",
    "ApiKey": "YOUR-API-KEY"
  }
}
```

The supported Azure resource host can also end in `.openai.azure.com`. Use the **Responses API endpoint ending in `/openai/v1/responses`**, not the project endpoint ending in `/api/projects/...`.

Restart the API after configuration changes. User Secrets are loaded in the **Development** environment. On the hosted server, use `Foundry__Enabled`, `Foundry__Endpoint`, `Foundry__Deployment` and `Foundry__ApiKey` environment variables instead.

The key stays on the backend. Never commit it, place it in frontend code or include it in a demo recording. Azure AI requests consume your Azure quota independently of hosting.

The UI displays successful generated reviews as **AZURE AI**. With AI disabled or an unavailable provider, it displays **AI not available here**; the match data, profiles and charts remain usable. Enabling AI with an incomplete or unsupported configuration prevents startup.

For deployment configuration, see [hosting and environment setup](docs/render-deploy.md).

## API overview

| Route | Purpose |
| --- | --- |
| `GET /health` | Service readiness |
| `GET /api/matches` | All bundled match scenarios |
| `GET /api/matches/{id}` | Match details, teams, players and result |
| `GET /api/matches/{id}/events` | Recorded event feed |
| `GET /api/matches/{id}/metrics` | Calculated match metrics |
| `GET /api/matches/{id}/recap` | Key moments; optional `playerId` filter |
| `GET /api/matches/{id}/decisions` | Decision snapshots, option measurements and heuristic assessments |
| `GET /api/reports/club?teamId=city` | Selected club and its tracked player profiles |
| `GET /api/reports?teamId=city&playerId=mci-9` | Player overview across the club's scenarios |
| `GET /api/reports?teamId=leeds&playerId=lee-5&matchId=leeds-sim-001` | Individual match report |
| `POST /api/reports/insight` | Player or club AI performance review |
| `POST /api/insights/generate` | AI analysis of selected recorded events or a decision option |

Use `teamId=leeds` or `teamId=city` for reports; Leeds is the default for clients that omit it. Omit `playerId` for tracked club totals. Report insight requests accept `teamId`, optional `playerId`, optional `matchId` and `audience` (`Player` or `Coach`). Event insight requests use `matchId`, `eventIds`, `audience` and an optional `optionId` belonging to the selected event.

## Verification

```powershell
dotnet test backend/Beyond90.sln
npm --prefix frontend test
npm --prefix frontend run build
npm --prefix frontend run lint
```

Tests cover event validation, analytics, decision geometry, AI evidence handling, club isolation, consistent player profiles, navigation and attribute visualizations.

## Scope and limitations

- Player attributes and season totals are supplied synthetic profiles; match metrics are calculated from synthetic recorded events.
- Decision diagrams show partial action-time positions. Option paths illustrate alternatives rather than replaying observed movement. Qualitative risk and option comparisons use uncalibrated rules, not success probabilities or expert scouting grades. Internal comparison weights are not presented as player grades.
- AI receives selected-event context, the available decision snapshot and option assessment, or backend-calculated report metrics. It does not watch match footage or reconstruct unseen positions.
- Generated explanations can contain unsupported interpretation even when the response passes evidence checks. Earlier evaluations and limitations are recorded in the [evaluation report](docs/evaluations/2026-10-23-gpt-4.1-mini.md); see the [evaluation tool](tools/Beyond90.Evaluation/README.md) for repeatable checks.
- Club emblems and jerseys are hand-drawn pixel-style SVG illustrations. No player photographs or Premier League match footage are bundled.

## License

License selection is pending before public submission.
