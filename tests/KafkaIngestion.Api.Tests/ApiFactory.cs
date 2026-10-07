using Confluent.Kafka;
using KafkaIngestion.Api.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KafkaIngestion.Api.Tests;

public sealed class ApiFactory(bool purgeEnabled = true, bool realKafka = false) : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-publication-key-only";
    public const string AdminKey = "test-administration-key-only";
    public string Topic { get; } = $"tests.ids.{Guid.NewGuid():N}";
    public FakeKafkaGateway Gateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Authentication:ApiKey"] = ApiKey,
                ["Authentication:AdminApiKey"] = AdminKey,
                ["Kafka:Topic"] = Topic,
                ["Kafka:EnablePurge"] = purgeEnabled.ToString(),
                ["Kafka:Partitions"] = "3",
                ["Kafka:BootstrapServers"] = Environment.GetEnvironmentVariable("KAFKA_INTEGRATION_BOOTSTRAP_SERVERS") ?? "localhost:19092"
            }));
        if (!realKafka)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IKafkaGateway>();
                services.AddSingleton<IKafkaGateway>(Gateway);
            });
        }
    }

    public HttpClient CreateAuthenticatedClient(string? key = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", key ?? ApiKey);
        return client;
    }
}

public sealed class FakeKafkaGateway : IKafkaGateway
{
    public List<string> PublishedIds { get; } = [];
    public bool FailPublish { get; set; }
    public bool FailReadiness { get; set; }
    public int PurgeCalls { get; private set; }
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<PublishedMessage> PublishAsync(string id, CancellationToken cancellationToken)
    {
        if (FailPublish) throw new KafkaException(new Error(ErrorCode.Local_MsgTimedOut));
        PublishedIds.Add(id);
        return Task.FromResult(new PublishedMessage(id, "fake.ids", 0, PublishedIds.Count - 1));
    }
    public Task<PurgedTopic> PurgeAsync(CancellationToken cancellationToken)
    {
        PurgeCalls++;
        return Task.FromResult(new PurgedTopic("fake.ids", [new PurgedPartition(0, PublishedIds.Count)]));
    }
    public Task CheckReadinessAsync(CancellationToken cancellationToken) => FailReadiness
        ? Task.FromException(new KafkaException(new Error(ErrorCode.Local_AllBrokersDown)))
        : Task.CompletedTask;
}
