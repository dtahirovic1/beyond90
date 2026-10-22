# Beyond90

**The Game Doesn't End at Full Time.**

Beyond90 turns synthetic football match events into explainable statistics and audience-specific match insights. The demo uses real Arsenal and Chelsea player names from the 2026/27 squads, but every event and statistic is invented for illustration; it is not a replay or claim about an actual match. It does not analyze video or use Premier League footage, club crests, or player images.

## Project status

The solution scaffold, event validation, reproducible synthetic match, and deterministic analytics are implemented. The match API, insights, and replay UI are the next milestones.

## Stack

- Frontend: React, TypeScript, Vite, Tailwind CSS, SVG
- Backend: ASP.NET Core Web API (.NET 10)
- Data: synthetic JSON events using real club/player names, explicitly labeled as an illustrative scenario
- AI: optional Microsoft Foundry inference through a backend-only provider; the demo must work without credentials

## Run locally

Start the API:

```powershell
dotnet run --project backend/Beyond90.Api
```

Start the frontend in another terminal:

```powershell
cd frontend
npm install
npm run dev
```

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
