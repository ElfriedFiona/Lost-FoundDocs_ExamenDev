using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.Models;

public sealed class UserAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    /// <summary>9 chiffres, sans préfixe (ex. 677124821).</summary>
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    /// <summary>SIMULATION (frontend uniquement) : le mot de passe est gardé en clair en mémoire. Un vrai backend ne le fera jamais.</summary>
    public string Password { get; set; } = string.Empty;
    public bool IsPhoneVerified { get; set; }
    public bool IsDemoAccount { get; init; }

    public string FirstName => FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? FullName;

    public string Initials => string.Concat(FullName
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(p => char.ToUpperInvariant(p[0])));

    public string DisplayPhone => $"+237 {PhoneNumber.Format(Phone)}";
}
