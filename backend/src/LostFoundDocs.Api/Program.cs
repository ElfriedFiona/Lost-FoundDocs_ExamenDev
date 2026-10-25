using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LostFoundDocs.Api.Data;
using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Endpoints;
using LostFoundDocs.Api.Infrastructure;
using LostFoundDocs.Api.Security;
using LostFoundDocs.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Options ----------
builder.Services.Configure<SecurityOptions>(config.GetSection("Security"));
builder.Services.Configure<SmsOptions>(config.GetSection("Sms"));
builder.Services.Configure<StorageOptions>(config.GetSection("Storage"));
builder.Services.Configure<MatchingOptions>(config.GetSection("Matching"));
builder.Services.Configure<RestitutionOptions>(config.GetSection("Restitution"));
builder.Services.Configure<PublicOptions>(config.GetSection("Public"));

var security = config.GetSection("Security").Get<SecurityOptions>() ?? new SecurityOptions();
var isProduction = builder.Environment.IsProduction();
if (isProduction && security.ServerSecret.Length < 32)
    throw new InvalidOperationException("Security:ServerSecret (32 caractères minimum) est obligatoire en production (variable d'environnement Security__ServerSecret).");
if (isProduction && security.ExposeOtpInResponse)
    throw new InvalidOperationException("Security:ExposeOtpInResponse ne doit jamais être activé en production.");
if (security.ServerSecret.Length < 16)
    throw new InvalidOperationException("Security:ServerSecret est vide ou trop court.");

// ---------- Données ----------
var storage = config.GetSection("Storage").Get<StorageOptions>() ?? new StorageOptions();
var connectionString = config.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var directory = Path.GetDirectoryName(Path.GetFullPath(storage.DatabasePath));
    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    connectionString = $"Data Source={storage.DatabasePath};Default Timeout=30";
}
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

// ---------- Services ----------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new Fingerprinter(sp.GetRequiredService<IOptions<SecurityOptions>>().Value.ServerSecret));
builder.Services.AddSingleton<PhotoStorage>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MatchQueries>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ReceiptService>();
builder.Services.AddScoped<RestitutionService>();

if (string.Equals(config["Sms:Provider"], "Http", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<ISmsSender, HttpSmsSender>(c => c.Timeout = TimeSpan.FromSeconds(10));
else
    builder.Services.AddSingleton<ISmsSender, LogSmsSender>();

if (config.GetValue("Background:Enabled", true)) builder.Services.AddHostedService<ExpiryWorker>();

// ---------- Authentification, erreurs, JSON ----------
builder.Services.AddAuthentication(SessionAuth.Scheme).AddScheme<AuthenticationSchemeOptions, SessionAuthHandler>(SessionAuth.Scheme, null);
builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.Converters.Add(new UtcDateTimeConverter());
});
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 12L * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 12L * 1024 * 1024);

// ---------- Limitation de débit ----------
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "about:blank", title = "Trop de requêtes", status = 429, code = "too_many_requests",
            detail = "Trop de requêtes. Patientez un instant avant de réessayer.",
        }, ct);
    };
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Max(1, security.AuthRequestsPerMinute), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("messages", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.Identity?.IsAuthenticated == true ? ctx.User.UserId().ToString() : ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Max(1, security.MessagesPerMinute), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

if (security.AllowedOrigins.Length > 0)
{
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(security.AllowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}
if (security.TrustForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

var app = builder.Build();

// ---------- Initialisation de la base ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (config.GetValue("Storage:EnableWal", true) && db.Database.IsSqlite())
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    Directory.CreateDirectory(storage.PhotosPath);
}

// ---------- Pipeline ----------
if (security.TrustForwardedHeaders) app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<ApiExceptionMiddleware>();
if (security.RequireHttps)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["Referrer-Policy"] = "no-referrer";
    h["X-Frame-Options"] = "DENY";
    h["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'";
    await next();
});
if (security.AllowedOrigins.Length > 0) app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapApi();
app.Run();

public partial class Program;
