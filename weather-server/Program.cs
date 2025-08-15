using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace WeatherServer;

// MCP Server Weather - tools, prompts, and resources

[McpServerToolType]
public static class WeatherTools
{
    [McpServerTool, Description("Get a daily weather forecast (rain, temperature, etc.) for a location and date.")]
    public static async Task<string> GetDailyForecast(
        [Description("Location name, e.g. Newcastle")] string location,
        [Description("Date to check, e.g. 2025-08-16")] DateTime date)
    {
        // Step 1: Geocode location
        using HttpClient? httpClient = new HttpClient();
        string? geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1";
        HttpResponseMessage? geoResponse = await httpClient.GetAsync(geoUrl);
        if (!geoResponse.IsSuccessStatusCode)
            return $"Could not find location: {location}";
        string? geoJson = await geoResponse.Content.ReadAsStringAsync();
        System.Text.Json.JsonDocument? geoData = System.Text.Json.JsonDocument.Parse(geoJson);
        if (!geoData.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return $"Could not find location: {location}";
        double lat = results[0].GetProperty("latitude").GetDouble();
        double lon = results[0].GetProperty("longitude").GetDouble();

        // Step 2: Query weather forecast for rain, temperature, etc.
        string? dateStr = date.ToString("yyyy-MM-dd");
        string? weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=rain_sum,temperature_2m_max,temperature_2m_min,precipitation_hours&timezone=auto&start_date={dateStr}&end_date={dateStr}";
        HttpResponseMessage? weatherResponse = await httpClient.GetAsync(weatherUrl);
        if (!weatherResponse.IsSuccessStatusCode)
            return $"Could not get weather data for {location} on {dateStr}";
        string? weatherJson = await weatherResponse.Content.ReadAsStringAsync();
        System.Text.Json.JsonDocument? weatherData = System.Text.Json.JsonDocument.Parse(weatherJson);
        if (!weatherData.RootElement.TryGetProperty("daily", out var daily))
            return $"Could not get daily forecast for {location} on {dateStr}";
        string summary = $"Weather forecast for {location} on {dateStr}: ";
        if (daily.TryGetProperty("rain_sum", out var rainArr))
        {
            double rain = rainArr[0].GetDouble();
            summary += $"Rain: {rain} mm. ";
        }
        if (daily.TryGetProperty("temperature_2m_max", out var tempMaxArr))
        {
            double tempMax = tempMaxArr[0].GetDouble();
            summary += $"Max Temp: {tempMax}°C. ";
        }
        if (daily.TryGetProperty("temperature_2m_min", out var tempMinArr))
        {
            double tempMin = tempMinArr[0].GetDouble();
            summary += $"Min Temp: {tempMin}°C. ";
        }
        if (daily.TryGetProperty("precipitation_hours", out var precipHoursArr))
        {
            double precipHours = precipHoursArr[0].GetDouble();
            summary += $"Precipitation hours: {precipHours}. ";
        }
        return summary.Trim();
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);
        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly()
            .WithPromptsFromAssembly()
            .WithResourcesFromAssembly();
        var app = builder.Build();
        await app.RunAsync();
    }
}
