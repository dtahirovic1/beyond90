using System.Text.Json.Serialization;
using Beyond90.Ai;
using Beyond90.Api;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddSingleton(MatchStore.LoadCatalog(Path.Combine(AppContext.BaseDirectory, "leeds")));
builder.Services.AddSingleton<DemoInsightProvider>();
builder.Services.AddOptions<FoundryOptions>()
    .BindConfiguration("Foundry")
    .Validate(options => options.IsValid(), "When Foundry is enabled, configure its Azure Responses endpoint, deployment, and API key in User Secrets.")
    .ValidateOnStart();
builder.Services.AddHttpClient("Foundry", client => client.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddTransient<IInsightProvider>(services =>
{
    var options = services.GetRequiredService<IOptions<FoundryOptions>>().Value;
    return options.Enabled
        ? new FoundryInsightProvider(services.GetRequiredService<IHttpClientFactory>().CreateClient("Foundry"),
            options.Endpoint, options.ApiKey, options.Deployment)
        : services.GetRequiredService<DemoInsightProvider>();
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program { }
