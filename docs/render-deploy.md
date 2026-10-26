# Deploy Beyond90 on Render

The root Dockerfile builds React and publishes the .NET 10 API into one service. Both the UI and `/api` use the same HTTPS origin. Match fixtures are bundled; no database or persistent disk is required.

## Create the service

1. Open [the Render Blueprint for this repository](https://dashboard.render.com/select-repo?type=blueprint&repo=https://github.com/dtahirovic1/beyond90).
2. Sign in, select your workspace, and connect the repository if prompted.
3. Review `render.yaml`: one **Free** Docker web service in Frankfurt, tracking `main`.
4. Deploy and wait until the service is **Live**. Use the actual `https://…onrender.com` address Render assigns.
5. Confirm `/health`, both clubs, a player profile and a match review load. Put the public application URL into Demosmith, not the GitHub repository URL.

Free services sleep after 15 minutes without traffic and can take about a minute to wake. Open the application and wait for it to load before starting a Demosmith capture. For the judging period, a paid instance can remove the free-service sleep limitation; changing to a paid plan is a separate choice.

## Enable Azure AI reviews

The default deployment shows the complete matchroom, but AI review controls report unavailable until Azure is configured. Local User Secrets are never copied to the image.

In **Render → service → Environment**, add these four variables together, then save and redeploy:

| Variable | Value |
| --- | --- |
| `Foundry__Enabled` | `true` |
| `Foundry__Endpoint` | Your Azure resource's `https://RESOURCE.services.ai.azure.com/openai/v1/responses` endpoint, or the supported `.openai.azure.com` equivalent |
| `Foundry__Deployment` | Your actual Azure deployment name; defaults to `gpt-4.1-mini` |
| `Foundry__ApiKey` | Your Azure API key, entered only in Render's server environment configuration |

Do not commit secrets, put them in frontend variables, or paste them into chat. Enabling AI without a valid endpoint, deployment and key intentionally fails startup. AI usage is billed by Azure independently of Render hosting.

## Updates and checks

Pushes to `main` trigger a new build. Render checks `/health` before routing traffic to the new instance. `/api/matches` must list eight fixtures, and `/api/reports/club?teamId=city` must contain the three City players. Unknown `/api/*` requests return 404 JSON rather than the SPA HTML.

To verify the production files locally without Docker, run `npm ci` and `npm run build` in `frontend`, publish `backend/Beyond90.Api` to `.deploy-check`, copy `frontend/dist` into `.deploy-check/wwwroot`, and run the published DLL with `.deploy-check` as its working directory.

Sources: [Render Docker deployments](https://render.com/docs/docker), [Blueprint specification](https://render.com/docs/blueprint-spec), [Free instance limitations](https://render.com/docs/free).
