using Microsoft.AspNetCore.Diagnostics;

namespace LostFoundDocs.Api.Infrastructure;

/// <summary>Erreurs métier : traitées avant le gestionnaire global pour ne pas être journalisées comme des erreurs serveur.</summary>
public sealed class ApiExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException e) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = e.Status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "about:blank", title = GlobalExceptionHandler.Title(e.Status), status = e.Status, code = e.Code, detail = e.Message, errors = e.Errors,
            });
        }
    }
}

/// <summary>Convertit toute erreur en réponse ProblemDetails (RFC 7807) ; les erreurs imprévues ne divulguent aucun détail interne.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        int status;
        object body;
        switch (exception)
        {
            case ApiException api:
                status = api.Status;
                body = new { type = "about:blank", title = Title(api.Status), status, code = api.Code, detail = api.Message, errors = api.Errors };
                break;
            case BadHttpRequestException bad:
                status = StatusCodes.Status400BadRequest;
                body = new { type = "about:blank", title = Title(400), status, code = "bad_request", detail = "Requête invalide." };
                logger.LogDebug(bad, "Requête invalide");
                break;
            case OperationCanceledException:
                return true;
            default:
                status = StatusCodes.Status500InternalServerError;
                body = new { type = "about:blank", title = Title(500), status, code = "server_error", detail = "Une erreur est survenue. Réessayez plus tard.", traceId = context.TraceIdentifier };
                logger.LogError(exception, "Erreur non gérée ({TraceId})", context.TraceIdentifier);
                break;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(body, ct);
        return true;
    }

    public static string Title(int status) => status switch
    {
        400 => "Requête invalide", 401 => "Authentification requise", 403 => "Action non autorisée", 404 => "Introuvable",
        409 => "Conflit", 429 => "Trop de requêtes", _ => "Erreur serveur",
    };
}
