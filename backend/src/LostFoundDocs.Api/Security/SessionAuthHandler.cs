using System.Security.Claims;
using System.Text.Encodings.Web;
using LostFoundDocs.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Security;

public static class SessionAuth
{
    public const string Scheme = "Session";
    public const string SessionIdClaim = "sid";

    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw Infrastructure.ApiException.Unauthorized();

    public static Guid SessionId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(SessionIdClaim), out var id) ? id : throw Infrastructure.ApiException.Unauthorized();
}

/// <summary>
/// Authentification par jeton d'accès opaque (en-tête « Authorization: Bearer … »). Le jeton est aléatoire (256 bits) et seul son
/// hachage SHA-256 est stocké : une fuite de base ne permet pas de se faire passer pour un utilisateur, et la déconnexion est immédiate.
/// </summary>
public sealed class SessionAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
                                       AppDbContext db, TimeProvider time)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();

        var hash = Tokens.Hash(header[7..].Trim());
        var now = time.GetUtcNow().UtcDateTime;
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.AccessTokenHash == hash && s.RevokedAt == null && s.AccessExpiresAt > now);
        if (session is null) return AuthenticateResult.Fail("Jeton invalide ou expiré.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == session.UserId && u.DeletedAt == null);
        if (user is null) return AuthenticateResult.Fail("Compte introuvable.");

        if (now - session.LastUsedAt > TimeSpan.FromMinutes(5))
        {
            session.LastUsedAt = now;
            await db.SaveChangesAsync();
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(SessionAuth.SessionIdClaim, session.Id.ToString()),
        }, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        Response.ContentType = "application/problem+json";
        await Response.WriteAsJsonAsync(new
        {
            type = "about:blank", title = "Authentification requise", status = 401, code = "unauthorized",
            detail = "Connectez-vous pour continuer.",
        });
    }
}
