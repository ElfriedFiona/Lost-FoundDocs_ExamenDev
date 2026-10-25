namespace LostFoundDocs.Services.Abstractions;

/// <summary>Service dont les données fictives en mémoire peuvent être réinitialisées (écran « À propos »).</summary>
public interface IResettable
{
    void Reset();
}
