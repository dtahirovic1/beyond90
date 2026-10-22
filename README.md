# Beyond90

**The Game Doesn't End at Full Time.**

Beyond90 turns fictional, synthetic football match events into explainable statistics and audience-specific match insights. The MVP demonstrates one reproducible match through Player, Coach, and Fan/Broadcast views. It does not analyze video or use real Premier League footage, clubs, or player identities.

## Project status

The initial solution scaffold and event-validation contract are in progress. The replay UI, analytics, API, and AI provider will be added in the next milestones.

## Stack

- Frontend: React, TypeScript, Vite, Tailwind CSS, SVG
- Backend: ASP.NET Core Web API (.NET 10)
- Data: fictional synthetic JSON/JSONL events
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

All match, player, and club names are fictional. No footage or unlicensed sports data is included. Numbers are computed by the backend; generated insight text must refer to event IDs and verified metric evidence. API credentials belong in backend environment configuration and must never be committed.

## License

License selection is pending before public submission.
