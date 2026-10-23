# Selected-event explanation evaluation

This small smoke evaluation calls the real configured Azure deployment. It is not a benchmark of general football expertise or proof of production reliability.

Method: task-specific examples and explicit qualitative criteria, following [OpenAI evaluation guidance](https://developers.openai.com/api/docs/guides/evaluation-best-practices). Automated evidence validation and qualitative prose review are reported separately.

## Fixed protocol (before viewing outputs)

- Baseline: provider/prompt from commit `752ef02`, original flat input contract, regression event `evt-001`, all three audiences (3 requests).
- Candidate: current production provider and controller, six selections, all three audiences (18 requests).
- Cases: completed pass (`evt-001`), unknown outcome and pressure (`evt-011`), saved shot (`evt-003`), incomplete pass (`evt-004`), turnover (`evt-005`), and two events (`evt-002`, `evt-003`) that do not establish an assist or causal connection.
- One sample per combination. Keep all failed responses. No retries, cherry-picking or automatic model switching. A second run is a separate evaluation.
- Maximum 21 calls, each with the production 800-output-token limit. Baseline and candidate use the same deployment. The report records response model, tokens and latency; it does not estimate billing from unsupported pricing assumptions.

## Review rubric

For every response, review the actual input, prose and caveats:

1. Correct player/team identity; opaque IDs must not become shirt numbers.
2. Correct event/outcome/pressure; missing fields remain unknown.
3. No attribution of both-team recorded totals to one player/team; no invented statistics, causes, assists, goals, or observations. Suggestions/questions must clearly be hypothetical.
4. Disclosure of synthetic and limited data.
5. Useful audience adaptation: fan = plain summary; player = action-focused reflection; coach = review questions with limits. Generic repeated caveats alone do not count as useful adaptation.

Provisional acceptance for this demo: all 18 candidate responses pass the API evidence checks, zero factual/identity/scope errors on review, all acknowledge material missing data, and at least 15/18 offer useful audience-appropriate explanations. Failing any factual gate requires revision before recommending the model for this task. Passing supports only this narrow demo; repeat on held-out scenarios and get human football-domain review before broader use. The assistant's qualitative review is not an independent human evaluation.

## Run

From the repository root, with `Beyond90-Local-Development` User Secrets already configured:

```powershell
dotnet run --project tools/Beyond90.Evaluation -- --live --output evaluation-results/run.json
```

The explicit `--live` switch prevents accidental paid calls. The runner reads the backend's User Secrets in-process, never prints credentials, refuses an invalid Azure endpoint, calls providers through the production controller, and counts a demo fallback as a failure. Results include only synthetic input context, generated outputs and usage metadata. Output files are ignored by Git; publish only reviewed, sanitized reports. No secrets are copied into reports. A live run cannot be substituted with mocked HTTP tests.
