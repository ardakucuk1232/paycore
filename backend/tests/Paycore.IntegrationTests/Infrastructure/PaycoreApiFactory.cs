using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Paycore.IntegrationTests.Infrastructure;

public sealed class PaycoreApiFactory(string postgresConnectionString, string redisConnectionString)
    : WebApplicationFactory<Program>
{
    public const string UnreachablePostgres = "Host=127.0.0.1;Port=1;Database=paycore;Username=paycore;Password=unused;Timeout=1";
    public const string UnreachableRedis = "127.0.0.1:1,connectTimeout=500,abortConnect=false";

    public static PaycoreApiFactory For(ContainersFixture containers) =>
        new(containers.PostgresConnectionString, containers.RedisConnectionString);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", postgresConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", redisConnectionString);
    }
}
