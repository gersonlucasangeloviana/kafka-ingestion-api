using System.ComponentModel.DataAnnotations;
using Confluent.Kafka;

namespace KafkaIngestion.Api.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    [Required] public string BootstrapServers { get; init; } = "localhost:9092";
    [Required, RegularExpression(@"[a-zA-Z0-9][a-zA-Z0-9._-]{0,248}")]
    public string Topic { get; init; } = "benchmark.ids";
    [Range(1, 128)] public int Partitions { get; init; } = 6;
    [Range(1, 10)] public short ReplicationFactor { get; init; } = 1;
    [Range(0, 100)] public int LingerMs { get; init; } = 5;
    [Range(1000, 120000)] public int MessageTimeoutMs { get; init; } = 10000;
    [Range(1000, 1000000)] public int QueueBufferingMaxMessages { get; init; } = 100000;
    public bool CreateTopicOnStartup { get; init; } = true;
    public bool EnablePurge { get; init; }
    public SecurityProtocol SecurityProtocol { get; init; } = SecurityProtocol.Plaintext;
    public SaslMechanism? SaslMechanism { get; init; }
    public string? SaslUsername { get; init; }
    public string? SaslPassword { get; init; }
    public string? SslCaLocation { get; init; }
}
