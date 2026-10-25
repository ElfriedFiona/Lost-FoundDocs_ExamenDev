using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

/// <summary>Déclaration d'un document trouvé : un seul écran, aucun champ indispensable sauf nom ou numéro visible.</summary>
public sealed class DeclareFoundViewModel : DeclarationViewModelBase
{
    public DeclareFoundViewModel(IReportService reports, INavigationService nav, IDialogService dialogs)
        : base(reports, nav, dialogs, ReportKind.Found)
    {
        SubmitCommand = new AsyncCommand(SubmitFormAsync);
        BackCommand = new AsyncCommand(LeaveAsync);
    }

    public ICommand SubmitCommand { get; }
    public ICommand BackCommand { get; }

    public string SubmitText => IsBusy ? "Enregistrement…" : "Déclarer le document trouvé";

    private async Task SubmitFormAsync()
    {
        FormError = string.Empty;
        var typeOk = ValidateType();
        var optionalOk = ValidateOptionalFields();

        // Règle de la maquette : au moins le nom OU le numéro visible (un numéro inconnu n'empêche pas de continuer).
        var identityOk = !string.IsNullOrWhiteSpace(HolderName) || !string.IsNullOrWhiteSpace(Number);
        NameError = identityOk ? string.Empty : "Renseignez au moins le nom ou le numéro visible sur le document.";

        var eventOk = ValidateEventFields("La date de découverte est obligatoire.");
        if (!(typeOk && optionalOk && identityOk && eventOk)) return;

        IsBusy = true;
        OnPropertyChanged(nameof(SubmitText));
        try { await SubmitAsync(); }
        finally { OnPropertyChanged(nameof(SubmitText)); }
    }
}
