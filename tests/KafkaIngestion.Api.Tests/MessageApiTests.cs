using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KafkaIngestion.Api.Messaging;

namespace KafkaIngestion.Api.Tests;

public sealed class MessageApiTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task Unauthenticated_requests_never_publish(string? key)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        if (key is not null) client.DefaultRequestHeaders.Add("X-Api-Key", key);
        using var response = await client.PostAsJsonAsync("/api/v1/messages", new { id = "123" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.Gateway.PublishedIds);
    }

    [Fact]
    public async Task Valid_id_returns_the_confirmed_offset()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        using var response = await client.PostAsJsonAsync("/api/v1/messages", new { id = "customer-123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PublishedMessage>();
        Assert.Equal("customer-123", result!.Id);
        Assert.Equal(0, result.Offset);
        Assert.Equal(["customer-123"], factory.Gateway.PublishedIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_ids_are_rejected(string? id)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        using var response = await client.PostAsJsonAsync("/api/v1/messages", new { id });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Gateway.PublishedIds);
    }

    [Fact]
    public async Task Validation_enforces_utf8_size_without_trimming_the_id()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        using var accepted = await client.PostAsJsonAsync("/api/v1/messages", new { id = new string('é', 128) });
        using var rejected = await client.PostAsJsonAsync("/api/v1/messages", new { id = new string('é', 129) });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Single(factory.Gateway.PublishedIds);
    }

    [Fact]
    public async Task Kafka_failure_never_returns_success_or_exposes_secrets()
    {
        await using var factory = new ApiFactory();
        factory.Gateway.FailPublish = true;
        using var client = factory.CreateAuthenticatedClient();
        using var response = await client.PostAsJsonAsync("/api/v1/messages", new { id = "private-id" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ApiFactory.ApiKey, body);
        Assert.DoesNotContain("private-id", body);
        Assert.Empty(factory.Gateway.PublishedIds);
    }

    [Fact]
    public async Task Readiness_fails_when_kafka_is_down_but_liveness_remains_healthy()
    {
        await using var factory = new ApiFactory();
        factory.Gateway.FailReadiness = true;
        using var client = factory.CreateClient();
        using var ready = await client.GetAsync("/health/ready");
        using var live = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    [Fact]
    public async Task Openapi_is_protected_and_describes_message_and_admin_routes()
    {
        await using var factory = new ApiFactory();
        using var anonymous = factory.CreateClient();
        using var denied = await anonymous.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var client = factory.CreateAuthenticatedClient();
        var document = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/v1/messages", document);
        Assert.Contains("/api/v1/admin/topic/records", document);
        Assert.Contains("X-Api-Key", document);
        using var json = JsonDocument.Parse(document);
        var security = json.RootElement.GetProperty("paths").GetProperty("/api/v1/messages")
            .GetProperty("post").GetProperty("security")[0];
        Assert.Equal(JsonValueKind.Array, security.GetProperty("ApiKey").ValueKind);
    }
}
