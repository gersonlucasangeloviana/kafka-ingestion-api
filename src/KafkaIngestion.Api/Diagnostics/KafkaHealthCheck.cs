using KafkaIngestion.Api.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KafkaIngestion.Api.Diagnostics;

public sealed class KafkaHealthCheck(IKafkaGateway kafka) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await kafka.CheckReadinessAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Kafka is unavailable.");
        }
    }
}
