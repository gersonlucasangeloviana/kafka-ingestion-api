using System.Net;

namespace KafkaIngestion.Api.Tests;

public sealed class AdministrationApiTests
{
    [Fact]
    public async Task Publication_key_cannot_delete_records()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add("X-Confirm-Topic", factory.Topic);
        using var response = await client.DeleteAsync("/api/v1/admin/topic/records");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.Gateway.PurgeCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-topic")]
    public async Task Administration_requires_explicit_topic_confirmation(string? topic)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateAuthenticatedClient(ApiFactory.AdminKey);
        if (topic is not null) client.DefaultRequestHeaders.Add("X-Confirm-Topic", topic);
        using var response = await client.DeleteAsync("/api/v1/admin/topic/records");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Gateway.PurgeCalls);
    }

    [Fact]
    public async Task Purge_must_be_enabled_explicitly()
    {
        await using var factory = new ApiFactory(purgeEnabled: false);
        using var client = factory.CreateAuthenticatedClient(ApiFactory.AdminKey);
        client.DefaultRequestHeaders.Add("X-Confirm-Topic", factory.Topic);
        using var response = await client.DeleteAsync("/api/v1/admin/topic/records");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.Gateway.PurgeCalls);
    }

    [Fact]
    public async Task Authorized_confirmed_request_purges_the_configured_topic()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateAuthenticatedClient(ApiFactory.AdminKey);
        client.DefaultRequestHeaders.Add("X-Confirm-Topic", factory.Topic);
        using var response = await client.DeleteAsync("/api/v1/admin/topic/records");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Gateway.PurgeCalls);
    }
}
