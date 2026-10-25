using System.Security.Cryptography;
using System.Text;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using LostFoundDocs.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

/// <summary>Codes à usage unique envoyés par SMS : 6 chiffres, expiration, nombre d'essais et de demandes limités, stockés hachés.</summary>
public sealed class OtpService(AppDbContext db, ISmsSender sms, Fingerprinter fingerprints, IOptions<SecurityOptions> security,
                               TimeProvider time, ILogger<OtpService> logger)
{
    private SecurityOptions Options => security.Value;

    /// <summary>Crée et envoie un code. Renvoie le code uniquement si Security:ExposeOtpInResponse est activé (développement).</summary>
    public async Task<string?> IssueAsync(string phone, OtpPurpose purpose)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var since = now.AddHours(-1);
        var recent = await db.OtpCodes.CountAsync(o => o.Phone == phone && o.CreatedAt > since);
        if (recent >= Options.OtpMaxPerHour)
            throw ApiException.TooMany("Trop de codes demandés. Réessayez dans une heure.");

        var previous = await db.OtpCodes.Where(o => o.Phone == phone && o.Purpose == purpose && o.ConsumedAt == null).ToListAsync();
        foreach (var o in previous) o.ConsumedAt = now;

        var code = Tokens.SixDigitCode();
        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(), Phone = phone, Purpose = purpose, CodeHash = fingerprints.Otp(Scope(phone, purpose), code),
            CreatedAt = now, ExpiresAt = now.AddMinutes(Options.OtpMinutes),
        });
        await db.SaveChangesAsync();

        try
        {
            await sms.SendAsync(phone, $"Lost&Found Docs : votre code de vérification est {code}. Il expire dans {Options.OtpMinutes} minutes. Ne le partagez jamais.");
        }
        catch (Exception ex)
        {
            // L'échec d'envoi ne doit pas révéler d'information ni bloquer le parcours : l'utilisateur peut redemander un code.
            logger.LogError(ex, "Envoi du SMS impossible pour une demande de code.");
        }
        return Options.ExposeOtpInResponse ? code : null;
    }

    public async Task<bool> VerifyAsync(string phone, OtpPurpose purpose, string code)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var otp = await db.OtpCodes
            .Where(o => o.Phone == phone && o.Purpose == purpose && o.ConsumedAt == null && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
        if (otp is null) return false;

        if (otp.Attempts >= Options.OtpMaxAttempts)
        {
            otp.ConsumedAt = now;
            await db.SaveChangesAsync();
            return false;
        }

        otp.Attempts++;
        var expected = Encoding.UTF8.GetBytes(otp.CodeHash);
        var actual = Encoding.UTF8.GetBytes(fingerprints.Otp(Scope(phone, purpose), code.Trim()));
        var ok = CryptographicOperations.FixedTimeEquals(expected, actual);
        if (ok) otp.ConsumedAt = now;
        await db.SaveChangesAsync();
        return ok;
    }

    private static string Scope(string phone, OtpPurpose purpose) => $"{phone}:{purpose}";
}
