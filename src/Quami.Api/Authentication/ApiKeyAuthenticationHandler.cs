using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Quami.Api.Authentication;

/// <summary>
/// API anahtarı doğrulaması. İstemci anahtarı <c>X-Api-Key</c> başlığında yollar.
/// Karşılaştırma sabit süreli yapılır (zamanlama sızıntısı olmasın).
/// Doğrulanan istemcinin adı kimliğe yazılır, böylece günlükte kimin çağırdığı görünür.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<ApiKeyOptions> apiKeyOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var provided) || provided.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        var presented = provided.ToString();
        if (string.IsNullOrWhiteSpace(presented))
            return Task.FromResult(AuthenticateResult.Fail("Boş API anahtarı."));

        var keys = apiKeyOptions.CurrentValue.Keys;
        if (keys.Count == 0)
        {
            Logger.LogWarning("API anahtarı tanımlı değil; korumalı uçlar kapalı.");
            return Task.FromResult(AuthenticateResult.Fail("API anahtarı tanımlı değil."));
        }

        foreach (var (clientName, expected) in keys)
        {
            if (string.IsNullOrWhiteSpace(expected) || !FixedTimeEquals(presented, expected))
                continue;

            var identity = new ClaimsIdentity(SchemeName);
            identity.AddClaim(new Claim(ClaimTypes.Name, clientName));
            var principal = new ClaimsPrincipal(identity);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
        }

        return Task.FromResult(AuthenticateResult.Fail("Geçersiz API anahtarı."));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = SchemeName;
        return Task.CompletedTask;
    }

    /// <summary>Uzunluk farkını da sızdırmayan sabit süreli karşılaştırma.</summary>
    private static bool FixedTimeEquals(string presented, string expected)
    {
        var a = Encoding.UTF8.GetBytes(presented);
        var b = Encoding.UTF8.GetBytes(expected);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
