using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using KafkaIngestion.Api.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace KafkaIngestion.Api.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ApiKeyOptions> keyOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string AdminPolicy = "Administrator";
    private readonly byte[] _apiKeyHash = Hash(keyOptions.Value.ApiKey);
    private readonly byte[] _adminKeyHash = Hash(keyOptions.Value.AdminApiKey);

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (values.Count != 1 || values[0] is not { Length: > 0 and <= 512 } suppliedKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var hash = Hash(suppliedKey);
        var isAdmin = CryptographicOperations.FixedTimeEquals(hash, _adminKeyHash);
        if (!isAdmin && !CryptographicOperations.FixedTimeEquals(hash, _apiKeyHash))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, isAdmin ? "administrator" : "producer"),
            new Claim(ClaimTypes.Role, isAdmin ? "Administrator" : "Producer")
        ], SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
