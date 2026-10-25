namespace LostFoundDocs.Api.Infrastructure;

/// <summary>Erreur métier convertie en réponse ProblemDetails (RFC 7807) par le gestionnaire global.</summary>
public sealed class ApiException : Exception
{
    public ApiException(int status, string code, string message, IDictionary<string, string[]>? errors = null) : base(message)
    {
        Status = status;
        Code = code;
        Errors = errors;
    }

    public int Status { get; }
    public string Code { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public static ApiException BadRequest(string code, string message, IDictionary<string, string[]>? errors = null) => new(400, code, message, errors);
    public static ApiException Unauthorized(string message = "Authentification requise.") => new(401, "unauthorized", message);
    public static ApiException Forbidden(string message = "Action non autorisée.") => new(403, "forbidden", message);
    public static ApiException NotFound(string message = "Ressource introuvable.") => new(404, "not_found", message);
    public static ApiException Conflict(string code, string message) => new(409, code, message);
    public static ApiException TooMany(string message) => new(429, "too_many_requests", message);
}
