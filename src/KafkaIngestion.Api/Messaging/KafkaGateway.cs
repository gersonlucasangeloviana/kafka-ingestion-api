using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaIngestion.Api.Configuration;
using Microsoft.Extensions.Options;

namespace KafkaIngestion.Api.Messaging;

public sealed class KafkaGateway : IKafkaGateway, IDisposable
{
    private static readonly TimeSpan AdminTimeout = TimeSpan.FromSeconds(15);
    private readonly KafkaOptions _options;
    private readonly IProducer<string, string> _producer;
    private readonly IAdminClient _admin;

    public KafkaGateway(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
        var client = new ClientConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = "kafka-ingestion-api",
            SecurityProtocol = _options.SecurityProtocol,
            SaslMechanism = _options.SaslMechanism,
            SaslUsername = _options.SaslUsername,
            SaslPassword = _options.SaslPassword,
            SslCaLocation = _options.SslCaLocation,
            SocketTimeoutMs = 10000
        };
        _admin = new AdminClientBuilder(new AdminClientConfig(client)).Build();
        _producer = new ProducerBuilder<string, string>(new ProducerConfig(client)
        {
            EnableIdempotence = true,
            Acks = Acks.All,
            CompressionType = CompressionType.Lz4,
            LingerMs = _options.LingerMs,
            MessageTimeoutMs = _options.MessageTimeoutMs,
            QueueBufferingMaxMessages = _options.QueueBufferingMaxMessages,
            QueueBufferingMaxKbytes = 65536,
            AllowAutoCreateTopics = false
        }).Build();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_options.CreateTopicOnStartup)
        {
            try
            {
                await _admin.CreateTopicsAsync([new TopicSpecification
                {
                    Name = _options.Topic,
                    NumPartitions = _options.Partitions,
                    ReplicationFactor = _options.ReplicationFactor,
                    Configs = new Dictionary<string, string>
                    {
                        ["cleanup.policy"] = "delete",
                        ["retention.ms"] = "3600000"
                    }
                }], new CreateTopicsOptions { RequestTimeout = AdminTimeout })
                    .WaitAsync(cancellationToken);
            }
            catch (CreateTopicsException exception) when (
                exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                // Existing topics retain their partition count, replication and retention settings.
            }
        }

        await CheckReadinessAsync(cancellationToken);
    }

    public async Task<PublishedMessage> PublishAsync(string id, CancellationToken cancellationToken)
    {
        var delivery = await _producer.ProduceAsync(_options.Topic,
            new Message<string, string> { Key = id, Value = id }, cancellationToken);
        if (delivery.Status != PersistenceStatus.Persisted)
        {
            throw new KafkaException(new Error(ErrorCode.Local_MsgTimedOut, "Delivery was not confirmed."));
        }

        return new PublishedMessage(id, delivery.Topic, delivery.Partition.Value, delivery.Offset.Value);
    }

    public async Task<PurgedTopic> PurgeAsync(CancellationToken cancellationToken)
    {
        var partitions = await GetPartitionsAsync(cancellationToken);
        // Capture numeric offsets: records appended after each snapshot are retained.
        var snapshot = await _admin.ListOffsetsAsync(partitions.Select(partition =>
            new TopicPartitionOffsetSpec { TopicPartition = partition, OffsetSpec = OffsetSpec.Latest() }),
            new ListOffsetsOptions { RequestTimeout = AdminTimeout }).WaitAsync(cancellationToken);
        var offsets = snapshot.ResultInfos.Select(result =>
        {
            var offset = result.TopicPartitionOffsetError;
            if (offset.Error.IsError)
            {
                throw new KafkaException(offset.Error);
            }

            return new TopicPartitionOffset(offset.TopicPartition, offset.Offset);
        }).ToArray();
        var deleted = await _admin.DeleteRecordsAsync(offsets, new DeleteRecordsOptions
        {
            RequestTimeout = AdminTimeout,
            OperationTimeout = TimeSpan.FromSeconds(10)
        }).WaitAsync(cancellationToken);
        return new PurgedTopic(_options.Topic, deleted.Select(result =>
            new PurgedPartition(result.Partition.Value,
                result.Offset.Value)).ToArray());
    }

    public async Task CheckReadinessAsync(CancellationToken cancellationToken) =>
        _ = await GetPartitionsAsync(cancellationToken);

    private Task<TopicPartition[]> GetPartitionsAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            var metadata = _admin.GetMetadata(_options.Topic, TimeSpan.FromSeconds(5));
            var topic = metadata.Topics.SingleOrDefault(topic => topic.Topic == _options.Topic);
            if (topic is null || topic.Error.IsError || topic.Partitions.Count == 0)
            {
                throw new KafkaException(topic?.Error ?? new Error(ErrorCode.UnknownTopicOrPart));
            }

            if (topic.Partitions.Any(partition => partition.Error.IsError || partition.Leader < 0))
            {
                throw new KafkaException(new Error(ErrorCode.LeaderNotAvailable));
            }

            return topic.Partitions.Select(partition =>
                new TopicPartition(_options.Topic, partition.PartitionId)).ToArray();
        }, cancellationToken);

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        _admin.Dispose();
    }
}
