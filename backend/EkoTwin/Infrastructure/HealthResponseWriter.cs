using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EkoTwin.Infrastructure;

/// <summary>
/// /health uç noktasının JSON çıktısı. Örnek:
/// { "status": "Healthy", "checks": [ { "name": "database", "status": "Healthy", "durationMs": 12 } ] }
/// Sağlıklıysa 200, değilse 503 döner.
/// </summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds),
                description = e.Value.Description
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
