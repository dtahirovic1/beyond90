using System.Text.Json;
using Beyond90.Ai;
using Beyond90.Api;
using Beyond90.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Beyond90.Ai.Tests;

public sealed class CityReportTests
{
    [Fact]
    public void City_catalogue_and_reports_are_isolated_from_Leeds()
    {
        var store = MatchStore.LoadCatalog(Path.Combine(AppContext.BaseDirectory, "leeds"), includeCity: true);
        Assert.Equal(8, store.All.Count);
        var controller = new ReportsController(store, new DemoInsightProvider(), new DemoInsightProvider());
        JsonElement Json(IActionResult response) => JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(response).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var club = Json(controller.Club("city"));
        Assert.Equal("Manchester City", club.GetProperty("name").GetString());
        Assert.Equal(3, club.GetProperty("players").GetArrayLength());
        foreach (var id in new[] { "mci-9", "mci-10", "mci-15" })
        {
            var report = Json(controller.Get(id, null, "city"));
            Assert.Equal(4, report.GetProperty("matchCount").GetInt32());
            Assert.All(report.GetProperty("matches").EnumerateArray(), match => Assert.StartsWith("city-", match.GetProperty("id").GetString()));
            Assert.Equal(store.All.SelectMany(m => m.Events).Count(e => e.PlayerId == id), report.GetProperty("stats").GetProperty("events").GetInt32());
        }
        Assert.IsType<NotFoundResult>(controller.Get("lee-9", null, "city"));
        Assert.IsType<NotFoundResult>(controller.Get(null, "leeds-sim-001", "city"));
        Assert.IsType<NotFoundResult>(controller.Get("mci-9", null, "leeds"));
        Assert.Equal(4, Json(controller.Get(null, null)).GetProperty("matchCount").GetInt32());
        Assert.All(store.All.Where(m => m.Id.StartsWith("city-")), m => Assert.NotEmpty(m.Decisions));
    }
}
