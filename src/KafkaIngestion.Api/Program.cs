using System.Threading.RateLimiting;
using KafkaIngestion.Api.Configuration;
using KafkaIngestion.Api.Diagnostics;
using KafkaIngestion.Api.Features.Administration;
using KafkaIngestion.Api.Features.Messages;
using KafkaIngestion.Api.Messaging;
using KafkaIngestion.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(server => server.Limits.MaxRequestBodySize = 4096);
builder.Services.AddOptions<ApiKeyOptions>().BindConfiguration(ApiKeyOptions.SectionName)
    .Validate(options => options.ApiKey.Length is >= 16 and <= 512, "API key must contain 16-512 characters.")
    .Validate(options => options.AdminApiKey.Length is >= 16 and <= 512, "Admin key must contain 16-512 characters.")
    .Validate(options => options.ApiKey != options.AdminApiKey, "API and admin keys must differ.")
    .ValidateOnStart();
builder.Services.AddOptions<KafkaOptions>().BindConfiguration(KafkaOptions.SectionName)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorizationBuilder().AddPolicy(ApiKeyAuthenticationHandler.AdminPolicy,
    policy => policy.RequireAuthenticatedUser().RequireRole("Administrator"));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton<IKafkaGateway, KafkaGateway>();
builder.Services.AddHostedService<KafkaStartupService>();
builder.Services.AddHealthChecks().AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddConcurrencyLimiter("ingestion", limiter =>
    {
        limiter.PermitLimit = builder.Configuration.GetValue("Ingestion:MaxConcurrentRequests", 8192);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddConcurrencyLimiter("administration", limiter =>
    {
        limiter.PermitLimit = 1;
        limiter.QueueLimit = 0;
    });
});
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer((operation, context, _) =>
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
        {
            operation.Security = [new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("ApiKey", context.Document)] = []
            }];
        }

        return Task.CompletedTask;
    });
    options.AddDocumentTransformer((document, _, _) =>
{
    document.Info = new OpenApiInfo
    {
        Title = "Kafka Ingestion API",
        Version = "v1",
        Description = "Minimal API for acknowledged Kafka ingestion and reproducible load testing. Supply X-Api-Key for protected endpoints."
    };
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
    {
        ["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationHandler.HeaderName
        }
    };
    return Task.CompletedTask;
});
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapMessageEndpoints();
app.MapTopicEndpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapOpenApi().RequireAuthorization();
app.Run();

public partial class Program;
