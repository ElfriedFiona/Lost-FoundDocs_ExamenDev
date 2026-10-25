using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LostFoundDocs.Api.Domain;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

public interface ISmsSender
{
    Task SendAsync(string phoneNineDigits, string message, CancellationToken ct = default);
}

/// <summary>Écrit le SMS dans les journaux (développement, tests). Ne jamais utiliser en production : les codes y apparaissent.</summary>
public sealed class LogSmsSender(ILogger<LogSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phoneNineDigits, string message, CancellationToken ct = default)
    {
        logger.LogWarning("[SMS simulé] vers {Phone} : {Message}", phoneNineDigits, message);
        return Task.CompletedTask;
    }
}

/// <summary>Envoie le SMS à une passerelle HTTP configurable (Sms:Url, Sms:BodyTemplate, en-tête d'authentification).</summary>
public sealed class HttpSmsSender(HttpClient http, IOptions<SmsOptions> options, ILogger<HttpSmsSender> logger) : ISmsSender
{
    public async Task SendAsync(string phoneNineDigits, string message, CancellationToken ct = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.Url)) throw new InvalidOperationException("Sms:Url n'est pas configuré.");

        var body = o.BodyTemplate
            .Replace("{to}", JsonEscape(o.CountryPrefix + phoneNineDigits))
            .Replace("{message}", JsonEscape(message));

        using var request = new HttpRequestMessage(HttpMethod.Post, o.Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (!string.IsNullOrWhiteSpace(o.AuthHeaderValue))
            request.Headers.TryAddWithoutValidation(o.AuthHeaderName, o.AuthHeaderValue);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Échec d'envoi SMS ({Status}).", (int)response.StatusCode);
            throw new HttpRequestException($"La passerelle SMS a répondu {(int)response.StatusCode}.");
        }
    }

    // Échappe une valeur pour l'insérer entre guillemets dans un gabarit JSON.
    private static string JsonEscape(string value) => JsonSerializer.Serialize(value)[1..^1];
}
