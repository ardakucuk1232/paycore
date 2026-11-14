using Paycore.Api.Configuration;
using Paycore.Api.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddPaycoreHealthChecks(
    postgresConnectionString: builder.Configuration.GetRequiredConnectionString("Postgres"),
    redisConnectionString: builder.Configuration.GetRequiredConnectionString("Redis"));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Paycore API v1"));

app.MapPaycoreHealthChecks();

app.Run();
