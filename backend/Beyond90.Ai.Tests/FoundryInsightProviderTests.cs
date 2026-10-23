using System.Net;
using System.Text;
using System.Text.Json;
using Beyond90.Domain;

namespace Beyond90.Ai.Tests;

public sealed class FoundryInsightProviderTests
{
    private const string Endpoint = "https://example.services.ai.azure.com/openai/v1/responses";
    private const string InsightJson = """
        {"insightId":"test","audience":"Fan","eventIds":["evt-003"],
         "metricEvidence":[{"metricKey":"events.selected","value":1,"eventIds":["evt-003"]}],
         "text":"The synthetic shot was saved.","caveats":["Synthetic events only."]}
        """;

    [Fact]
    public async Task Sends_responses_contract_and_reads_output_after_non_message_items()
    {
        string? sentBody = null;
        using var client = new HttpClient(new Handler(async (message, token) =>
        {
            Assert.Equal(Endpoint, message.RequestUri!.AbsoluteUri);
            Assert.Equal(HttpMethod.Post, message.Method);
            Assert.Equal("test-secret", message.Headers.GetValues("api-key").Single());
            Assert.Null(message.Headers.Authorization);
            sentBody = await message.Content!.ReadAsStringAsync(token);
            return JsonResponse(Envelope(InsightJson));
        }));
        var provider = new FoundryInsightProvider(client, Endpoint, "test-secret", "my-deployment");

        var insight = await provider.GenerateAsync(Request(), CancellationToken.None);

        Assert.Equal("The synthetic shot was saved.", insight.Text);
        Assert.Equal(Audience.Fan, insight.Audience);
        Assert.Equal("evt-003", Assert.Single(insight.EventIds));
        using var json = JsonDocument.Parse(sentBody!);
        var body = json.RootElement;
        Assert.Equal("my-deployment", body.GetProperty("model").GetString());
        Assert.False(body.GetProperty("store").GetBoolean());
        Assert.InRange(body.GetProperty("max_output_tokens").GetInt32(), 1, 1200);
        Assert.Equal("json_schema", body.GetProperty("text").GetProperty("format").GetProperty("type").GetString());
        Assert.True(body.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean());
        Assert.False(body.TryGetProperty("messages", out _));
        Assert.Contains("evt-003", body.GetProperty("input").GetString());
        Assert.DoesNotContain("test-secret", sentBody);
    }

    [Theory]
    [InlineData("incomplete")]
    [InlineData("failed")]
    public async Task Does_not_accept_partial_or_failed_output_even_if_json_looks_valid(string status)
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(JsonResponse(Envelope(InsightJson, status)))));
        var provider = new FoundryInsightProvider(client, Endpoint, "test-secret", "my-deployment");
        await Assert.ThrowsAsync<InsightProviderException>(() => provider.GenerateAsync(Request(), CancellationToken.None));
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        using var client = new HttpClient(new Handler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return JsonResponse("{}");
        }));
        var provider = new FoundryInsightProvider(client, Endpoint, "test-secret", "my-deployment");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GenerateAsync(Request(), cancellation.Token));
    }

    internal static InsightRequest Request() => new("demo", ["evt-003"], Audience.Fan,
        new Dictionary<string, decimal?> { ["events.selected"] = 1 },
        [new MatchEvent { EventId = "evt-003", Type = "Shot", Outcome = "saved", Minute = 3 }]);

    internal static string Envelope(string text, string status = "completed") => JsonSerializer.Serialize(new
    {
        id = "resp_test", status,
        output = new object[] {
            new { type = "reasoning", summary = Array.Empty<object>() },
            new { type = "message", role = "assistant", status = "completed", content = new[] {
                new { type = "output_text", text, annotations = Array.Empty<object>() }
            } }
        }
    });

    internal static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    internal sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
