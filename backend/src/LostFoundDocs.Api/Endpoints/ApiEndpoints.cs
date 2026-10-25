using LostFoundDocs.Api.Contracts;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using LostFoundDocs.Api.Security;
using LostFoundDocs.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        MapAuth(api);
        MapProfile(api);
        MapReports(api);
        MapMatches(api);
        MapConversations(api);
        MapNotifications(api);
        MapRestitution(api);
        MapReceipts(api);
        MapPublic(app, api);
    }

    private static string? Ip(HttpContext c) => c.Connection.RemoteIpAddress?.ToString();
    private static string? UserAgent(HttpContext c) => c.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;

    // ---------- Authentification ----------
    private static void MapAuth(RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").RequireRateLimiting("auth");

        auth.MapPost("/register", async (RegisterRequest req, AuthService s, HttpContext c) =>
            Results.Json(await s.RegisterAsync(req, Ip(c), UserAgent(c)), statusCode: StatusCodes.Status201Created));

        auth.MapPost("/login", async (LoginRequest req, AuthService s, HttpContext c) =>
            Results.Ok(await s.LoginAsync(req, Ip(c), UserAgent(c))));

        auth.MapPost("/refresh", async (RefreshRequest req, AuthService s) => Results.Ok(await s.RefreshAsync(req)));

        auth.MapPost("/logout", async (HttpContext c, AuthService s) =>
        {
            await s.LogoutAsync(c.User.SessionId());
            return Results.NoContent();
        }).RequireAuthorization();

        auth.MapPost("/phone/send-code", async (HttpContext c, AuthService s) =>
            Results.Ok(await s.ResendPhoneCodeAsync(c.User.UserId()))).RequireAuthorization();

        auth.MapPost("/phone/verify", async (VerifyPhoneRequest req, HttpContext c, AuthService s) =>
            Results.Ok(await s.VerifyPhoneAsync(c.User.UserId(), req))).RequireAuthorization();

        auth.MapPost("/password/forgot", async (ForgotPasswordRequest req, AuthService s) =>
            Results.Json(await s.ForgotPasswordAsync(req), statusCode: StatusCodes.Status202Accepted));

        auth.MapPost("/password/verify-code", async (VerifyResetCodeRequest req, AuthService s) =>
            Results.Ok(await s.VerifyResetCodeAsync(req)));

        auth.MapPost("/password/reset", async (ResetPasswordRequest req, AuthService s, HttpContext c) =>
        {
            await s.ResetPasswordAsync(req, Ip(c));
            return Results.NoContent();
        });
    }

    // ---------- Profil ----------
    private static void MapProfile(RouteGroupBuilder api)
    {
        var me = api.MapGroup("/me").RequireAuthorization();

        me.MapGet("", async (HttpContext c, AuthService s) => Results.Ok(await s.GetMeAsync(c.User.UserId())));
        me.MapPut("", async (UpdateProfileRequest req, HttpContext c, AuthService s) => Results.Ok(await s.UpdateProfileAsync(c.User.UserId(), req)));
        me.MapPost("/password", async (ChangePasswordRequest req, HttpContext c, AuthService s) =>
        {
            await s.ChangePasswordAsync(c.User.UserId(), c.User.SessionId(), req, Ip(c));
            return Results.NoContent();
        });
        me.MapDelete("", async ([FromBody] DeleteAccountRequest req, HttpContext c, AuthService s) =>
        {
            await s.DeleteAccountAsync(c.User.UserId(), req, Ip(c));
            return Results.NoContent();
        });
    }

    // ---------- Signalements ----------
    private static void MapReports(RouteGroupBuilder api)
    {
        var reports = api.MapGroup("/reports").RequireAuthorization();

        reports.MapPost("", async (CreateReportRequest req, HttpContext c, ReportService s) =>
        {
            var (report, created) = await s.CreateAsync(c.User.UserId(), req);
            return created ? Results.Created($"/api/v1/reports/{report.Id}", report) : Results.Ok(report);
        });

        reports.MapGet("", async (HttpContext c, ReportService s, ReportKind? kind, ReportStatus? status) =>
            Results.Ok(await s.ListAsync(c.User.UserId(), kind, status)));

        reports.MapGet("/{id:guid}", async (Guid id, HttpContext c, ReportService s) => Results.Ok(await s.GetAsync(c.User.UserId(), id)));
        reports.MapPost("/{id:guid}/cancel", async (Guid id, HttpContext c, ReportService s) => Results.Ok(await s.CancelAsync(c.User.UserId(), id)));
        reports.MapPost("/{id:guid}/resolve-recovered", async (Guid id, HttpContext c, ReportService s) =>
            Results.Ok(await s.ResolveRecoveredAsync(c.User.UserId(), id)));
        reports.MapPost("/{id:guid}/search", async (Guid id, HttpContext c, MatchingService m) => Results.Ok(await m.SearchAsync(c.User.UserId(), id)));

        reports.MapPost("/{id:guid}/photo", async (Guid id, HttpContext c, ReportService s, CancellationToken ct) =>
        {
            if (!c.Request.HasFormContentType) throw ApiException.BadRequest("photo_format", "Envoyez la photo en multipart/form-data (champ « file »).");
            var form = await c.Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? throw ApiException.BadRequest("photo_missing", "Aucun fichier reçu (champ « file »).");
            await using var stream = file.OpenReadStream();
            await s.SavePhotoAsync(c.User.UserId(), id, stream, ct);
            return Results.NoContent();
        }).DisableAntiforgery();

        reports.MapGet("/{id:guid}/photo", async (Guid id, HttpContext c, ReportService s) =>
        {
            var (stream, contentType) = await s.OpenPhotoAsync(c.User.UserId(), id);
            return Results.Stream(stream, contentType);
        });
    }

    // ---------- Correspondances ----------
    private static void MapMatches(RouteGroupBuilder api)
    {
        var matches = api.MapGroup("/matches").RequireAuthorization();

        matches.MapGet("", async (HttpContext c, MatchQueries queries, Data.AppDbContext db) =>
        {
            var userId = c.User.UserId();
            var mine = await db.Reports.Where(r => r.UserId == userId).ToListAsync();
            var current = await queries.CurrentMatchesAsync(mine);
            var result = new List<MatchDto>();
            foreach (var m in current.Values.DistinctBy(m => m.Id)) result.Add(await queries.BuildDtoAsync(m, userId));
            return Results.Ok(result);
        });

        matches.MapGet("/{id:guid}", async (Guid id, HttpContext c, MatchQueries queries) =>
        {
            var userId = c.User.UserId();
            var (match, _, _) = await queries.LoadForParticipantAsync(userId, id);
            return Results.Ok(await queries.BuildDtoAsync(match, userId));
        });

        matches.MapPost("/{id:guid}/dispute", async (Guid id, DisputeRequest req, HttpContext c, MatchingService m) =>
            Results.Ok(await m.DisputeAsync(c.User.UserId(), id, req)));
    }

    // ---------- Messagerie ----------
    private static void MapConversations(RouteGroupBuilder api)
    {
        var conv = api.MapGroup("/conversations").RequireAuthorization();

        conv.MapGet("", async (HttpContext c, ConversationService s) => Results.Ok(await s.ListAsync(c.User.UserId())));
        conv.MapGet("/{id:guid}/messages", async (Guid id, HttpContext c, ConversationService s, DateTime? after, int? limit) =>
            Results.Ok(await s.MessagesAsync(c.User.UserId(), id, after?.ToUniversalTime(), limit ?? 200)));
        conv.MapPost("/{id:guid}/messages", async (Guid id, SendMessageRequest req, HttpContext c, ConversationService s) =>
            Results.Json(await s.SendAsync(c.User.UserId(), id, req), statusCode: StatusCodes.Status201Created)).RequireRateLimiting("messages");
        conv.MapPost("/{id:guid}/read", async (Guid id, HttpContext c, ConversationService s) =>
        {
            await s.MarkReadAsync(c.User.UserId(), id);
            return Results.NoContent();
        });
    }

    // ---------- Notifications ----------
    private static void MapNotifications(RouteGroupBuilder api)
    {
        var n = api.MapGroup("/notifications").RequireAuthorization();

        n.MapGet("", async (HttpContext c, NotificationService s, DateTime? since, int? limit) =>
            Results.Ok(await s.ListAsync(c.User.UserId(), since?.ToUniversalTime(), limit ?? 100)));
        n.MapGet("/unread-count", async (HttpContext c, NotificationService s) => Results.Ok(new { count = await s.UnreadCountAsync(c.User.UserId()) }));
        n.MapPost("/read-all", async (HttpContext c, NotificationService s) =>
        {
            await s.MarkAllReadAsync(c.User.UserId());
            return Results.NoContent();
        });
        n.MapPost("/{id:guid}/read", async (Guid id, HttpContext c, NotificationService s) =>
        {
            await s.MarkReadAsync(c.User.UserId(), id);
            return Results.NoContent();
        });
    }

    // ---------- Restitution ----------
    private static void MapRestitution(RouteGroupBuilder api)
    {
        api.MapGet("/restitution/places", (RestitutionService s) => Results.Ok(s.Places())).RequireAuthorization();

        var r = api.MapGroup("/matches/{matchId:guid}/restitution").RequireAuthorization();
        r.MapGet("", async (Guid matchId, HttpContext c, RestitutionService s) => Results.Ok(await s.GetAsync(c.User.UserId(), matchId)));
        r.MapPost("/propose", async (Guid matchId, ProposeRequest req, HttpContext c, RestitutionService s) =>
            Results.Ok(await s.ProposeAsync(c.User.UserId(), matchId, req)));
        r.MapPost("/accept", async (Guid matchId, HttpContext c, RestitutionService s) => Results.Ok(await s.AcceptAsync(c.User.UserId(), matchId)));
        r.MapPost("/confirm", async (Guid matchId, HttpContext c, RestitutionService s) => Results.Ok(await s.ConfirmAsync(c.User.UserId(), matchId)));
    }

    // ---------- Reçus ----------
    private static void MapReceipts(RouteGroupBuilder api)
    {
        var r = api.MapGroup("/matches/{matchId:guid}/receipt").RequireAuthorization();
        r.MapGet("", async (Guid matchId, HttpContext c, ReceiptService s) => Results.Ok(await s.GetForParticipantAsync(c.User.UserId(), matchId)));
        r.MapGet("/pdf", async (Guid matchId, HttpContext c, ReceiptService s) =>
        {
            var bytes = await s.BuildPdfAsync(c.User.UserId(), matchId);
            return Results.File(bytes, "application/pdf", "recu-restitution.pdf");
        });
    }

    // ---------- Public : vérification des reçus, santé ----------
    private static void MapPublic(WebApplication app, RouteGroupBuilder api)
    {
        api.MapGet("/receipts/verify/{code}", async (string code, ReceiptService s) =>
        {
            var result = await s.VerifyAsync(code);
            return result is null ? Results.Json(new { valid = false }, statusCode: StatusCodes.Status404NotFound) : Results.Ok(result);
        }).RequireRateLimiting("auth");

        app.MapGet("/verify/{code}", async (string code, ReceiptService s, IOptions<RestitutionOptions> o) =>
        {
            var r = await s.VerifyAsync(code);
            return Results.Content(VerifyPage.Render(r, o.Value.UtcOffsetHours), "text/html; charset=utf-8", statusCode: r is null ? 404 : 200);
        }).RequireRateLimiting("auth");

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    }
}
