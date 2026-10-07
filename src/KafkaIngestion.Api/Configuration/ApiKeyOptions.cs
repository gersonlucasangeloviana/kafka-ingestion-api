namespace KafkaIngestion.Api.Configuration;

public sealed class ApiKeyOptions
{
    public const string SectionName = "Authentication";
    public string ApiKey { get; init; } = string.Empty;
    public string AdminApiKey { get; init; } = string.Empty;
}
