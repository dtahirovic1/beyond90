using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Beyond90.Ai;
using Beyond90.Api;
using Beyond90.Api.Controllers;
using Beyond90.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

if (!args.Contains("--live") || !args.Contains("--output"))
{
    Console.WriteLine("No calls made. Usage: --live --output evaluation-results/run.json (up to 15 paid Azure requests)");
    return 1;
}
var outputIndex = Array.IndexOf(args, "--output") + 1;
if (outputIndex >= args.Length) return 1;
var outputPath = Path.GetFullPath(args[outputIndex]);
if (File.Exists(outputPath))
{
    Console.WriteLine("Choose a new output path; existing evaluation results will not be overwritten.");
    return 1;
}

FoundryOptions options;
try
{
    var configuration = new ConfigurationBuilder().AddUserSecrets("Beyond90-Local-Development")
        .AddEnvironmentVariables().Build();
    options = configuration.GetSection("Foundry").Get<FoundryOptions>() ?? new();
    if (!options.Enabled || !options.IsValid())
    {
        Console.WriteLine("No calls made. Configure valid Foundry settings in backend User Secrets and enable Foundry.");
        return 1;
    }
}
catch
{
    Console.WriteLine("No calls made. Could not load valid backend User Secrets.");
    return 1;
}

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true, Converters = { new JsonStringEnumConverter() }
};
var fixturePath = Path.Combine(AppContext.BaseDirectory, "match-demo.json");
var store = MatchStore.Load(fixturePath);
var cases = new (string Name, string[] EventIds)[]
{
    ("identity-and-total-scope", ["evt-001"]),
    ("missing-outcome-and-pressure", ["evt-011"]),
    ("saved-shot", ["evt-003"]),
    ("incomplete-pass", ["evt-004"]),
    ("turnover", ["evt-005"]),
    ("multiple-events-no-proven-assist", ["evt-002", "evt-003"])
};
var rows = new List<object>();
var startedAt = DateTimeOffset.UtcNow;
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

foreach (var variant in new[] { "baseline-752ef02", "scoped-context-v2" })
{
    foreach (var scenario in variant.StartsWith("baseline") ? cases.Take(1) : cases)
    foreach (var audience in new[] { Audience.Player, Audience.Coach })
    {
        if (cancellation.IsCancellationRequested) return 1;
        using var capture = new UsageHandler(new HttpClientHandler { AllowAutoRedirect = false });
        using var client = new HttpClient(capture) { Timeout = TimeSpan.FromSeconds(30) };
        IInsightProvider provider = variant.StartsWith("baseline")
            ? new BaselineInsightProvider(client, options.Endpoint, options.ApiKey, options.Deployment)
            : new FoundryInsightProvider(client, options.Endpoint, options.ApiKey, options.Deployment);
        var controller = new InsightsController(store, provider, new DemoInsightProvider(), NullLogger<InsightsController>.Instance);
        var timer = Stopwatch.StartNew();
        Insight? insight = null;
        int? apiStatus = null;
        string? failure = null;
        try
        {
            var result = await controller.Generate(new GenerateInsightRequest
            {
                MatchId = store.Id, EventIds = scenario.EventIds, Audience = audience
            }, cancellation.Token);
            apiStatus = (result as ObjectResult)?.StatusCode;
            insight = (result as OkObjectResult)?.Value as Insight;
            if (insight?.Source != "Foundry") failure = "No accepted Foundry insight (rejected, unavailable, or demo fallback).";
        }
        catch (Exception exception)
        {
            // No exception message/body: authentication material must never enter reports.
            failure = exception.GetType().Name;
        }
        rows.Add(new
        {
            variant, scenario = scenario.Name, scenario.EventIds, audience,
            latencyMs = timer.ElapsedMilliseconds, apiStatus, capture.UpstreamStatus,
            capture.Model, capture.Usage, capture.Context, capture.GeneratedOutput, capture.PromptSha256, insight, failure,
            automaticEvidencePass = insight?.Source == "Foundry" && apiStatus == 200,
            qualitativeReview = "Pending review against the rubric; automatic evidence checks do not grade prose."
        });
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(new
        {
            startedAt, updatedAt = DateTimeOffset.UtcNow, deployment = options.Deployment,
            fixtureSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fixturePath))),
            maxCalls = 21, completedCalls = rows.Count, rows
        }, jsonOptions));
        Console.WriteLine($"{rows.Count}/21 {variant} {scenario.Name} {audience}: HTTP {capture.UpstreamStatus}, evidence {(failure is null ? "accepted" : "FAILED")}");
        // Pace requests for small development quotas. No retry can silently increase spending.
        if (rows.Count < 21)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(8), cancellation.Token); }
            catch (OperationCanceledException) { return 1; }
        }
    }
}
Console.WriteLine($"Saved {rows.Count} responses. Qualitative review is still required.");
return 0;

sealed class UsageHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    public JsonElement? Usage { get; private set; }
    public JsonElement? Context { get; private set; }
    public string? Model { get; private set; }
    public JsonElement? GeneratedOutput { get; private set; }
    public string? PromptSha256 { get; private set; }
    public int? UpstreamStatus { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        using (var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token)))
        {
            using var context = JsonDocument.Parse(body.RootElement.GetProperty("input").GetString()!);
            Context = context.RootElement.Clone();
            PromptSha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
                body.RootElement.GetProperty("instructions").GetString()!)));
        }
        var response = await base.SendAsync(request, token);
        UpstreamStatus = (int)response.StatusCode;
        if (response.IsSuccessStatusCode)
        {
            // Buffering here preserves the content for the production provider's parser.
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            if (body.RootElement.TryGetProperty("model", out var model)) Model = model.GetString();
            if (body.RootElement.TryGetProperty("usage", out var usage)) Usage = usage.Clone();
            if (body.RootElement.TryGetProperty("output", out var output)) GeneratedOutput = output.Clone();
        }
        return response;
    }
}
