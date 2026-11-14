namespace Paycore.Api.Configuration;

internal static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. Set the 'ConnectionStrings__{name}' environment variable.");
        }

        return connectionString;
    }
}
