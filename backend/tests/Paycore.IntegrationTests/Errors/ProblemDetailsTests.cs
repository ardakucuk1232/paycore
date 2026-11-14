using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Paycore.IntegrationTests.Infrastructure;

namespace Paycore.IntegrationTests.Errors;

public sealed class ProblemDetailsTests(ContainersFixture containers)
{
    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetailsBody()
    {
        await using var factory = PaycoreApiFactory.For(containers);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/v1/does-not-exist", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }
}
