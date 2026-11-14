using System.Net;
using System.Text.Json;
using Paycore.IntegrationTests.Infrastructure;

namespace Paycore.IntegrationTests.Health;

public sealed class HealthEndpointTests(ContainersFixture containers)
{
    [Fact]
    public async Task Readiness_IsHealthy_WhenPostgresAndRedisAreReachable()
    {
        await using var factory = PaycoreApiFactory.For(containers);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var report = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", report.GetProperty("status").GetString());
        Assert.Equal("Healthy", report.GetProperty("checks").GetProperty("postgres").GetString());
        Assert.Equal("Healthy", report.GetProperty("checks").GetProperty("redis").GetString());
    }

    [Fact]
    public async Task Readiness_IsUnavailable_AndNamesTheFailingDependency_WhenRedisIsDown()
    {
        await using var factory = new PaycoreApiFactory(containers.PostgresConnectionString, PaycoreApiFactory.UnreachableRedis);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var report = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Healthy", report.GetProperty("checks").GetProperty("postgres").GetString());
        Assert.Equal("Unhealthy", report.GetProperty("checks").GetProperty("redis").GetString());
    }

    [Fact]
    public async Task Liveness_StaysHealthy_WhenEveryDependencyIsDown()
    {
        await using var factory = new PaycoreApiFactory(PaycoreApiFactory.UnreachablePostgres, PaycoreApiFactory.UnreachableRedis);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var report = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", report.GetProperty("status").GetString());
        Assert.Empty(report.GetProperty("checks").EnumerateObject());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var body = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        return document.RootElement.Clone();
    }
}
