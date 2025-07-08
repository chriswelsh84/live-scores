
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add HttpClient for API calls
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// API endpoint to fetch fixtures
app.MapGet("/api/fixtures", async (IHttpClientFactory clientFactory) =>
{
    try
    {
        var client = clientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-rapidapi-host", "v3.football.api-sports.io");
        client.DefaultRequestHeaders.Add("x-rapidapi-key", "c70d109710msh6b880d57c84498bp19648ajsndcacc10a1d99");

        var today = "2025-07-08";
        var response = await client.GetAsync($"https://v3.football.api-sports.io/fixtures?date={today}");

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem($"API error: {response.StatusCode} {response.ReasonPhrase}", statusCode: (int)response.StatusCode);
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var fixtures = data.GetProperty("response").EnumerateArray().Select(fixture => new
        {
            id = fixture.GetProperty("fixture").GetProperty("id").GetInt32(),
            league = fixture.GetProperty("league").GetProperty("name").GetString(),
            home_team = fixture.GetProperty("teams").GetProperty("home").GetProperty("name").GetString(),
            away_team = fixture.GetProperty("teams").GetProperty("away").GetProperty("name").GetString(),
            score = new
            {
                home = fixture.GetProperty("goals").GetProperty("home").ValueKind == JsonValueKind.Null ? 0 : fixture.GetProperty("goals").GetProperty("home").GetInt32(),
                away = fixture.GetProperty("goals").GetProperty("away").ValueKind == JsonValueKind.Null ? 0 : fixture.GetProperty("goals").GetProperty("away").GetInt32()
            },
            status = fixture.GetProperty("fixture").GetProperty("status").GetProperty("long").GetString(),
            time = fixture.GetProperty("fixture").GetProperty("status").GetProperty("elapsed").ValueKind == JsonValueKind.Null ? "0'" : $"{fixture.GetProperty("fixture").GetProperty("status").GetProperty("elapsed").GetInt32()}'",
            kickoff = DateTime.Parse(fixture.GetProperty("fixture").GetProperty("date").GetString()).ToString("HH:mm")
        }).ToList();

        return Results.Ok(fixtures);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to fetch fixtures: {ex.Message}", statusCode: 500);
    }
});

app.Run();
