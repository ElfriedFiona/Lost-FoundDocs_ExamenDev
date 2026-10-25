namespace LostFoundDocs.Services.Abstractions;

public interface IDialogService
{
    Task AlertAsync(string title, string message, string okText = "OK");
    Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText = "Annuler");

    /// <summary>Feuille d'actions : renvoie le choix de l'utilisateur, ou null s'il annule.</summary>
    Task<string?> ChooseAsync(string title, string cancelText, params string[] options);
}
