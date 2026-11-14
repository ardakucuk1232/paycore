using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Paycore.Api.Health;

internal static class HealthCheckEndpoints
{
    private const string LivenessPath = "/health/live";
    private const string ReadinessPath = "/health/ready";
    private const string ReadinessTag = "ready";
    private static readonly TimeSpan DependencyCheckTimeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddPaycoreHealthChecks(
        this IServiceCollection services,
        string postgresConnectionString,
        string redisConnectionString)
    {
        services.AddHealthChecks()
            .AddNpgSql(postgresConnectionString, name: "postgres", tags: [ReadinessTag], timeout: DependencyCheckTimeout)
            .AddRedis(redisConnectionString, name: "redis", tags: [ReadinessTag], timeout: DependencyCheckTimeout);

        return services;
    }

    public static IEndpointRouteBuilder MapPaycoreHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteReportAsync,
        });

        endpoints.MapHealthChecks(ReadinessPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadinessTag),
            ResponseWriter = WriteReportAsync,
        });

        return endpoints;
    }

    private static Task WriteReportAsync(HttpContext context, HealthReport report)
    {
        var response = new HealthReportResponse(
            Status: report.Status.ToString(),
            Checks: report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString()));

        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    private sealed record HealthReportResponse(string Status, IReadOnlyDictionary<string, string> Checks);
}
