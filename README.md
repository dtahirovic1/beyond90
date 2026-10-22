# Beyond90

**The Game Doesn't End at Full Time.**

Beyond90 turns synthetic football match events into explainable statistics and audience-specific match insights. The demo uses real Arsenal and Chelsea player names from the 2026/27 squads, but every event and statistic is invented for illustration; it is not a replay or claim about an actual match. It does not analyze video or use Premier League footage, club crests, or player images.

## Project status

The solution scaffold, event validation, reproducible synthetic match, deterministic analytics, read-only match API, demo insight endpoint, and first match dashboard are implemented. A live AI provider and richer replay controls are later milestones.

## Stack

- Frontend: React, TypeScript, Vite, Tailwind CSS, SVG
- Backend: ASP.NET Core Web API (.NET 10)
- Data: synthetic JSON events using real club/player names, explicitly labeled as an illustrative scenario
- Insights: audience-specific demo response; external AI integration is a later milestone

## Run locally

Start the API:

```powershell
dotnet run --project backend/Beyond90.Api
```

The API serves the bundled synthetic match through these read-only routes:

- `GET /api/matches` — list available matches
- `GET /api/matches/{id}` — match details, teams, players, and data-status label
- `GET /api/matches/{id}/events` — ordered synthetic events
- `GET /api/matches/{id}/metrics` — calculated match metrics
- `GET /api/matches/{id}/recap` — key moments with supporting events
- `POST /api/insights/generate` — audience-specific demo insight for selected event IDs

The insight request body contains a `matchId`, an `eventIds` array, and an `audience` (`Player`, `Coach`, or `Fan`). The endpoint derives metrics from the fixture and rejects unknown event IDs. Its current provider returns a clearly labeled demo explanation and requires no external AI credentials.

Start the frontend in another terminal:

```powershell
cd frontend
npm install
npm run dev
```

The frontend requests match data through the Vite `/api` proxy, which expects the API at `http://127.0.0.1:5089`.

Run backend tests:

```powershell
dotnet test backend/Beyond90.sln
```

Run frontend checks:

```powershell
npm test --prefix frontend -- --run
npm run build --prefix frontend
```

## Data and safety

Player and club names identify real Premier League participants; all match events, outcomes, and statistics in the demo are synthetic and are not actual performance records. No logos, footage, or player images are included. Numbers are computed by the backend; generated insight text must refer to event IDs and verified metric evidence. API credentials belong in backend environment configuration and must never be committed. The roster reference is the Premier League 2026/27 squad list.

## License

License selection is pending before public submission.
