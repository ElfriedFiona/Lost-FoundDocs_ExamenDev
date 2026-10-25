using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using LostFoundDocs.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

public sealed class AuthService(AppDbContext db, TimeProvider time, IOptions<SecurityOptions> security, OtpService otp, AuditService audit)
{
    private SecurityOptions Options => security.Value;
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public static UserDto ToDto(User u) => new(u.Id, u.FullName, u.Phone, u.Email, u.PhoneVerifiedAt is not null, u.CreatedAt);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, string? ip, string? userAgent)
    {
        var v = new Validator();
        v.FullName("fullName", req.FullName);
        v.Phone("phone", req.Phone);
        v.OptionalEmail("email", req.Email);
        v.NewPassword("password", req.Password);
        v.ThrowIfInvalid();

        var phone = PhoneNumber.Normalize(req.Phone);
        if (await db.Users.AnyAsync(u => u.Phone == phone))
            throw ApiException.Conflict("phone_in_use", "Ce numéro est déjà associé à un compte.");

        var user = new User
        {
            Id = Guid.NewGuid(), FullName = req.FullName!.Trim(), Phone = phone,
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            PasswordHash = PasswordHasher.Hash(req.Password!, Options.Pbkdf2Iterations), CreatedAt = Now,
        };
        db.Users.Add(user);
        audit.Record(user.Id, "register", null, ip);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Inscription simultanée du même numéro : la contrainte d'unicité de la base fait foi.
            db.ChangeTracker.Clear();
            if (await db.Users.AnyAsync(u => u.Phone == phone))
                throw ApiException.Conflict("phone_in_use", "Ce numéro est déjà associé à un compte.");
            throw;
        }

        var tokens = await CreateSessionAsync(user, userAgent);
        var devOtp = await otp.IssueAsync(phone, OtpPurpose.PhoneVerification);
        return new AuthResponse(ToDto(user), tokens, devOtp);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, string? ip, string? userAgent)
    {
        var v = new Validator();
        v.Phone("phone", req.Phone);
        if (string.IsNullOrEmpty(req.Password)) v.Add("password", "Le mot de passe est obligatoire.");
        v.ThrowIfInvalid();

        var phone = PhoneNumber.Normalize(req.Phone);
        var now = Now;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone && u.DeletedAt == null);

        if (user is null)
        {
            PasswordHasher.BurnTime(req.Password!, Options.Pbkdf2Iterations);
            throw GenericLoginFailure();
        }

        if (user.LockedUntil is { } until && until > now)
        {
            audit.Record(user.Id, "login_locked", null, ip);
            await db.SaveChangesAsync();
            throw ApiException.TooMany($"Trop de tentatives. Réessayez dans {Math.Max(1, (int)Math.Ceiling((until - now).TotalMinutes))} minute(s).");
        }

        if (!PasswordHasher.Verify(req.Password!, user.PasswordHash, out var needsRehash, Options.Pbkdf2Iterations))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= Options.MaxFailedLogins)
            {
                user.LockedUntil = now.AddMinutes(Options.LockoutMinutes);
                user.FailedLoginCount = 0;
                audit.Record(user.Id, "account_locked", null, ip);
            }
            audit.Record(user.Id, "login_failed", null, ip);
            await db.SaveChangesAsync();
            throw GenericLoginFailure();
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        if (needsRehash) user.PasswordHash = PasswordHasher.Hash(req.Password!, Options.Pbkdf2Iterations);
        audit.Record(user.Id, "login", null, ip);
        await db.SaveChangesAsync();

        return new AuthResponse(ToDto(user), await CreateSessionAsync(user, userAgent), null);
    }

    private static ApiException GenericLoginFailure() =>
        new(401, "invalid_credentials", "Numéro ou mot de passe incorrect.");

    public async Task<TokenDto> RefreshAsync(RefreshRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) throw ApiException.Unauthorized("Jeton de rafraîchissement manquant.");
        var hash = Tokens.Hash(req.RefreshToken);
        var now = Now;
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash && s.RevokedAt == null && s.RefreshExpiresAt > now);
        if (session is null) throw ApiException.Unauthorized("Session expirée : reconnectez-vous.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == session.UserId && u.DeletedAt == null);
        if (user is null) throw ApiException.Unauthorized("Session expirée : reconnectez-vous.");

        var access = Tokens.Generate();
        var refresh = Tokens.Generate();
        session.AccessTokenHash = Tokens.Hash(access);
        session.AccessExpiresAt = now.AddMinutes(Options.AccessTokenMinutes);
        session.RefreshTokenHash = Tokens.Hash(refresh);
        session.RefreshExpiresAt = now.AddDays(Options.RefreshTokenDays);
        session.LastUsedAt = now;
        await db.SaveChangesAsync();
        return new TokenDto(access, session.AccessExpiresAt, refresh, session.RefreshExpiresAt);
    }

    public async Task LogoutAsync(Guid sessionId)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null) return;
        session.RevokedAt = Now;
        await db.SaveChangesAsync();
    }

    // ---------- Profil ----------
    public async Task<UserDto> GetMeAsync(Guid userId) => ToDto(await RequireUser(userId));

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest req)
    {
        var v = new Validator();
        v.FullName("fullName", req.FullName);
        v.OptionalEmail("email", req.Email);
        v.ThrowIfInvalid();
        var user = await RequireUser(userId);
        user.FullName = req.FullName!.Trim();
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        await db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(Guid userId, Guid currentSessionId, ChangePasswordRequest req, string? ip)
    {
        var v = new Validator();
        if (string.IsNullOrEmpty(req.CurrentPassword)) v.Add("currentPassword", "Le mot de passe actuel est obligatoire.");
        v.NewPassword("newPassword", req.NewPassword);
        if (v.IsValid && req.NewPassword == req.CurrentPassword) v.Add("newPassword", "Le nouveau mot de passe doit être différent de l’actuel.");
        v.ThrowIfInvalid();

        var user = await RequireUser(userId);
        if (!PasswordHasher.Verify(req.CurrentPassword!, user.PasswordHash, out _, Options.Pbkdf2Iterations))
        {
            audit.Record(userId, "change_password_denied", null, ip);
            await db.SaveChangesAsync();
            throw ApiException.BadRequest("invalid_current_password", "Mot de passe actuel incorrect.",
                new Dictionary<string, string[]> { ["currentPassword"] = new[] { "Mot de passe actuel incorrect." } });
        }

        user.PasswordHash = PasswordHasher.Hash(req.NewPassword!, Options.Pbkdf2Iterations);
        await RevokeSessionsAsync(userId, exceptSessionId: currentSessionId);
        audit.Record(userId, "change_password", null, ip);
        await db.SaveChangesAsync();
    }

    public async Task<OtpSentResponse> ResendPhoneCodeAsync(Guid userId)
    {
        var user = await RequireUser(userId);
        if (user.PhoneVerifiedAt is not null) throw ApiException.Conflict("already_verified", "Ce numéro est déjà vérifié.");
        var dev = await otp.IssueAsync(user.Phone, OtpPurpose.PhoneVerification);
        return new OtpSentResponse(true, dev);
    }

    public async Task<UserDto> VerifyPhoneAsync(Guid userId, VerifyPhoneRequest req)
    {
        var user = await RequireUser(userId);
        if (string.IsNullOrWhiteSpace(req.Code)) throw InvalidCode();
        if (!await otp.VerifyAsync(user.Phone, OtpPurpose.PhoneVerification, req.Code)) throw InvalidCode();
        user.PhoneVerifiedAt = Now;
        await db.SaveChangesAsync();
        return ToDto(user);
    }

    // ---------- Mot de passe oublié ----------
    public async Task<OtpSentResponse> ForgotPasswordAsync(ForgotPasswordRequest req)
    {
        var v = new Validator();
        v.Phone("phone", req.Phone);
        v.ThrowIfInvalid();
        var phone = PhoneNumber.Normalize(req.Phone);

        // Réponse identique que le compte existe ou non (pas d'énumération de comptes).
        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone && u.DeletedAt == null);
        string? dev = null;
        if (user is not null)
        {
            try { dev = await otp.IssueAsync(phone, OtpPurpose.PasswordReset); }
            catch (ApiException e) when (e.Status == 429) { /* silencieux */ }
        }
        return new OtpSentResponse(true, dev);
    }

    public async Task<ResetTokenResponse> VerifyResetCodeAsync(VerifyResetCodeRequest req)
    {
        var v = new Validator();
        v.Phone("phone", req.Phone);
        v.ThrowIfInvalid();
        var phone = PhoneNumber.Normalize(req.Phone);
        if (string.IsNullOrWhiteSpace(req.Code)) throw InvalidCode();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone && u.DeletedAt == null);
        var ok = user is not null && await otp.VerifyAsync(phone, OtpPurpose.PasswordReset, req.Code);
        if (!ok) throw InvalidCode();

        var token = Tokens.Generate();
        db.ResetTokens.Add(new ResetToken { Id = Guid.NewGuid(), UserId = user!.Id, TokenHash = Tokens.Hash(token), ExpiresAt = Now.AddMinutes(15) });
        await db.SaveChangesAsync();
        return new ResetTokenResponse(token);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req, string? ip)
    {
        var v = new Validator();
        if (string.IsNullOrWhiteSpace(req.ResetToken)) v.Add("resetToken", "Jeton manquant.");
        v.NewPassword("newPassword", req.NewPassword);
        v.ThrowIfInvalid();

        var hash = Tokens.Hash(req.ResetToken!);
        var now = Now;
        var token = await db.ResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > now);
        if (token is null) throw ApiException.BadRequest("invalid_reset_token", "La vérification a expiré. Recommencez depuis le début.");

        var user = await RequireUser(token.UserId);
        user.PasswordHash = PasswordHasher.Hash(req.NewPassword!, Options.Pbkdf2Iterations);
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        token.UsedAt = now;
        await RevokeSessionsAsync(user.Id, null);
        audit.Record(user.Id, "password_reset", null, ip);
        await db.SaveChangesAsync();
    }

    // ---------- Suppression du compte ----------
    public async Task DeleteAccountAsync(Guid userId, DeleteAccountRequest req, string? ip)
    {
        if (string.IsNullOrEmpty(req.Password)) throw ApiException.BadRequest("password_required", "Le mot de passe est obligatoire.");
        var user = await RequireUser(userId);
        if (!PasswordHasher.Verify(req.Password, user.PasswordHash, out _, Options.Pbkdf2Iterations))
            throw ApiException.BadRequest("invalid_current_password", "Mot de passe incorrect.");

        var now = Now;
        user.FullName = "Compte supprimé";
        user.Email = null;
        user.Phone = "deleted-" + user.Id.ToString("N");
        user.PasswordHash = "!";
        user.DeletedAt = now;
        await RevokeSessionsAsync(userId, null);
        audit.Record(userId, "account_deleted", null, ip);
        await db.SaveChangesAsync();
    }

    // ---------- Utilitaires ----------
    private async Task<User> RequireUser(Guid userId) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null) ?? throw ApiException.Unauthorized();

    private static ApiException InvalidCode() => ApiException.BadRequest("invalid_code", "Code incorrect ou expiré.",
        new Dictionary<string, string[]> { ["code"] = new[] { "Code incorrect ou expiré." } });

    private async Task<TokenDto> CreateSessionAsync(User user, string? userAgent)
    {
        var now = Now;
        var access = Tokens.Generate();
        var refresh = Tokens.Generate();
        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, AccessTokenHash = Tokens.Hash(access), AccessExpiresAt = now.AddMinutes(Options.AccessTokenMinutes),
            RefreshTokenHash = Tokens.Hash(refresh), RefreshExpiresAt = now.AddDays(Options.RefreshTokenDays), CreatedAt = now, LastUsedAt = now,
            UserAgent = userAgent is { Length: > 200 } ? userAgent[..200] : userAgent,
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return new TokenDto(access, session.AccessExpiresAt, refresh, session.RefreshExpiresAt);
    }

    private async Task RevokeSessionsAsync(Guid userId, Guid? exceptSessionId)
    {
        var now = Now;
        var sessions = await db.Sessions.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync();
        foreach (var s in sessions.Where(s => s.Id != exceptSessionId)) s.RevokedAt = now;
    }
}
