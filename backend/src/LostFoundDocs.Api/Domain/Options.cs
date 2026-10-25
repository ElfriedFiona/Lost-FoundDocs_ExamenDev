namespace LostFoundDocs.Api.Domain;

public sealed class SecurityOptions
{
    /// <summary>Clé secrète serveur (≥ 32 caractères) : empreintes des numéros/dates, codes SMS. Obligatoire hors développement.</summary>
    public string ServerSecret { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
    public int Pbkdf2Iterations { get; set; } = 210_000;
    public int MaxFailedLogins { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int OtpMinutes { get; set; } = 10;
    public int OtpMaxAttempts { get; set; } = 5;
    public int OtpMaxPerHour { get; set; } = 5;
    public int AuthRequestsPerMinute { get; set; } = 30;
    public int MessagesPerMinute { get; set; } = 20;
    /// <summary>Développement uniquement : renvoie le code SMS dans la réponse HTTP (jamais en production).</summary>
    public bool ExposeOtpInResponse { get; set; }
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    /// <summary>À activer uniquement derrière un reverse proxy de confiance (lit X-Forwarded-For / X-Forwarded-Proto).</summary>
    public bool TrustForwardedHeaders { get; set; }
    public bool RequireHttps { get; set; }
}

public sealed class SmsOptions
{
    /// <summary>"Log" (écrit le SMS dans les journaux) ou "Http" (passerelle SMS configurable).</summary>
    public string Provider { get; set; } = "Log";
    public string CountryPrefix { get; set; } = "+237";
    public string Url { get; set; } = "";
    public string AuthHeaderName { get; set; } = "Authorization";
    public string AuthHeaderValue { get; set; } = "";
    /// <summary>Corps JSON envoyé à la passerelle ; {to} et {message} sont remplacés (valeurs échappées en JSON).</summary>
    public string BodyTemplate { get; set; } = "{\"to\":\"{to}\",\"message\":\"{message}\"}";
}

public sealed class StorageOptions
{
    public string DatabasePath { get; set; } = "data/lostfound.db";
    public string PhotosPath { get; set; } = "data/photos";
    public int MaxPhotoMegabytes { get; set; } = 10;
}

public sealed class MatchingOptions
{
    public int MinScore { get; set; } = 55;
    public int StrongScore { get; set; } = 85;
    public int MaxGapDays { get; set; } = 180;
    public int LostDateToleranceDays { get; set; } = 7;
}

public sealed class RestitutionOptions
{
    public int UtcOffsetHours { get; set; } = 1;
    public int FirstHour { get; set; } = 7;
    public int LastHour { get; set; } = 19;
    public int MaxDaysAhead { get; set; } = 14;
    public MeetingPlaceOption[] Places { get; set; } =
    {
        new() { Name = "Commissariat du 11e arrondissement", District = "Bonamoussadi" },
        new() { Name = "Hôtel de ville de Douala 1er", District = "Bonanjo" },
        new() { Name = "Entrée principale, Université de Douala", District = "Campus 1" },
    };
}

public sealed class MeetingPlaceOption
{
    public string Name { get; set; } = "";
    public string District { get; set; } = "";
}

public sealed class PublicOptions
{
    /// <summary>URL publique de l'API (utilisée dans l'URL de vérification du reçu / QR code).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5080";
}
