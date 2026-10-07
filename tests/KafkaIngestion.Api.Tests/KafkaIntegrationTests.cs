using System.Net;
using System.Net.Http.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaIngestion.Api.Messaging;

namespace KafkaIngestion.Api.Tests;

public sealed class KafkaIntegrationTests
{
    [KafkaFact]
    [Trait("Category", "KafkaIntegration")]
    public async Task Published_ids_are_consumable_and_purge_advances_all_low_watermarks()
    {
        await using var factory = new ApiFactory(realKafka: true);
        using var client = factory.CreateAuthenticatedClient();
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_INTEGRATION_BOOTSTRAP_SERVERS"),
            GroupId = $"integration-{Guid.NewGuid():N}",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_INTEGRATION_BOOTSTRAP_SERVERS")
        }).Build();
        try
        {
            using var response = await client.PostAsJsonAsync("/api/v1/messages", new { id = "integration-123" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var published = (await response.Content.ReadFromJsonAsync<PublishedMessage>())!;
            var partition = new TopicPartition(factory.Topic, published.Partition);
            consumer.Assign(new TopicPartitionOffset(partition, published.Offset));
            var consumed = consumer.Consume(TimeSpan.FromSeconds(10));
            Assert.NotNull(consumed);
            Assert.Equal(published.Id, consumed.Message.Value);
            Assert.Equal(published.Id, consumed.Message.Key);

            using var administrator = factory.CreateAuthenticatedClient(ApiFactory.AdminKey);
            administrator.DefaultRequestHeaders.Add("X-Confirm-Topic", factory.Topic);
            using var purged = await administrator.DeleteAsync("/api/v1/admin/topic/records");
            Assert.Equal(HttpStatusCode.OK, purged.StatusCode);
            var result = (await purged.Content.ReadFromJsonAsync<PurgedTopic>())!;
            Assert.Equal(3, result.Partitions.Count);
            foreach (var deleted in result.Partitions)
            {
                var watermark = consumer.QueryWatermarkOffsets(
                    new TopicPartition(factory.Topic, deleted.Partition), TimeSpan.FromSeconds(10));
                Assert.Equal(watermark.High.Value, watermark.Low.Value);
                Assert.Equal(watermark.Low.Value, deleted.LowWatermark);
            }

            using var next = await client.PostAsJsonAsync("/api/v1/messages", new { id = published.Id });
            var republished = (await next.Content.ReadFromJsonAsync<PublishedMessage>())!;
            Assert.Equal(HttpStatusCode.OK, next.StatusCode);
            Assert.True(republished.Offset > published.Offset);
        }
        finally
        {
            consumer.Close();
            await admin.DeleteTopicsAsync([factory.Topic], new DeleteTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(15) });
        }
    }
}

public sealed class KafkaFactAttribute : FactAttribute
{
    public KafkaFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KAFKA_INTEGRATION_BOOTSTRAP_SERVERS")))
        {
            Skip = "Set KAFKA_INTEGRATION_BOOTSTRAP_SERVERS to run against a real broker.";
        }
    }
}
