namespace KafkaIngestion.Api.Messaging;

public interface IKafkaGateway
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<PublishedMessage> PublishAsync(string id, CancellationToken cancellationToken);
    Task<PurgedTopic> PurgeAsync(CancellationToken cancellationToken);
    Task CheckReadinessAsync(CancellationToken cancellationToken);
}

public sealed record PublishedMessage(string Id, string Topic, int Partition, long Offset);
public sealed record PurgedPartition(int Partition, long LowWatermark);
public sealed record PurgedTopic(string Topic, IReadOnlyList<PurgedPartition> Partitions);
