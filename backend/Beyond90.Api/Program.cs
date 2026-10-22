using System.Text.Json.Serialization;
using Beyond90.Api;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddSingleton(MatchStore.Load(Path.Combine(AppContext.BaseDirectory, "match-demo.json")));

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program { }
