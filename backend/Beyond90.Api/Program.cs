using System.Text.Json.Serialization;
using Beyond90.Ai;
using Beyond90.Api;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddSingleton(MatchStore.Load(Path.Combine(AppContext.BaseDirectory, "match-demo.json")));
builder.Services.AddSingleton<DemoInsightProvider>();
builder.Services.AddSingleton<IInsightProvider>(services => services.GetRequiredService<DemoInsightProvider>());

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program { }
