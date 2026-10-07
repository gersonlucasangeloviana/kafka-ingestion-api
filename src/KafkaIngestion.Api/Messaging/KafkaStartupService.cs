namespace KafkaIngestion.Api.Messaging;

public sealed class KafkaStartupService(IKafkaGateway kafka) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => kafka.InitializeAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
