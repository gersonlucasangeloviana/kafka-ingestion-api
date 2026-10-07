using KafkaIngestion.Api.Configuration;
using KafkaIngestion.Api.Messaging;
using KafkaIngestion.Api.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace KafkaIngestion.Api.Features.Administration;

public static class TopicEndpoints
{
    public static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/v1/admin/topic/records", PurgeAsync)
            .RequireAuthorization(ApiKeyAuthenticationHandler.AdminPolicy)
            .RequireRateLimiting("administration")
            .WithName("PurgeTopicRecords")
            .WithTags("Administration")
            .WithSummary("Delete records up to a captured offset in each topic partition.")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        return endpoints;
    }

    private static async Task<Results<Ok<PurgedTopic>, NotFound, ProblemHttpResult>> PurgeAsync(
        HttpRequest request, IKafkaGateway kafka, IOptions<KafkaOptions> options,
        CancellationToken cancellationToken)
    {
        if (!options.Value.EnablePurge)
        {
            return TypedResults.NotFound();
        }

        if (request.Headers["X-Confirm-Topic"].Count != 1 ||
            request.Headers["X-Confirm-Topic"][0] != options.Value.Topic)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Provide X-Confirm-Topic with the configured topic name.");
        }

        return TypedResults.Ok(await kafka.PurgeAsync(cancellationToken));
    }
}
