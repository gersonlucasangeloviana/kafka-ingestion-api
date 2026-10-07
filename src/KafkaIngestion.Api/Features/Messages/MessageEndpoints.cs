using System.Text;
using KafkaIngestion.Api.Messaging;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KafkaIngestion.Api.Features.Messages;

public static class MessageEndpoints
{
    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/messages", PublishAsync)
            .RequireAuthorization()
            .RequireRateLimiting("ingestion")
            .WithName("PublishMessage")
            .WithTags("Messages")
            .WithSummary("Publish an ID and await the Kafka delivery acknowledgement.")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);
        return endpoints;
    }

    private static async Task<Results<Ok<PublishedMessage>, ValidationProblem>> PublishAsync(
        PublishMessageRequest request, IKafkaGateway kafka, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || Encoding.UTF8.GetByteCount(request.Id) > 256)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["id"] = ["ID must be nonblank and contain at most 256 UTF-8 bytes."]
            });
        }

        return TypedResults.Ok(await kafka.PublishAsync(request.Id, cancellationToken));
    }
}

public sealed record PublishMessageRequest(string? Id);
