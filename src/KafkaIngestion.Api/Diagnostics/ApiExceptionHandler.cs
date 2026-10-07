using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KafkaIngestion.Api.Diagnostics;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var unavailable = exception is KafkaException;
        var status = unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        // Do not log request payloads, API keys or broker exception messages.
        logger.LogError("Request failed with {ExceptionType}; status {StatusCode}",
            exception.GetType().Name, status);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = unavailable ? "Kafka operation could not be confirmed." : "An unexpected error occurred.",
                Detail = unavailable ? "Publication or deletion may have occurred. Verify before retrying." : null
            }
        });
    }
}
