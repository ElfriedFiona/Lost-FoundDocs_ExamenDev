using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

/// <summary>Déclaration d'un document perdu en 4 étapes : type, identité, circonstances, photo et vérification.</summary>
public sealed class DeclareLostViewModel : DeclarationViewModelBase
{
    private int _step = 1;

    public DeclareLostViewModel(IReportService reports, INavigationService nav, IDialogService dialogs)
        : base(reports, nav, dialogs, ReportKind.Lost)
    {
        NextCommand = new AsyncCommand(NextAsync);
        BackCommand = new AsyncCommand(BackAsync);
        EditCommand = new Command(() => Step = 2);
    }

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand EditCommand { get; }

    public int Step
    {
        get => _step;
        private set
        {
            if (!SetProperty(ref _step, value)) return;
            foreach (var n in new[] { nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(IsStep4), nameof(NextText) })
                OnPropertyChanged(n);
        }
    }

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool IsStep4 => Step == 4;
    public string NextText => IsBusy ? "Enregistrement…" : Step == 4 ? "Déclarer le document perdu" : "Continuer";

    public override string TypeHint =>
        "nom complet, date de naissance, numéro, puis le lieu et la date de perte.";

    private async Task NextAsync()
    {
        FormError = string.Empty;
        switch (Step)
        {
            case 1:
                if (ValidateType()) Step = 2;
                break;
            case 2:
                var nameError = FieldRules.FullName(HolderName);
                NameError = nameError ?? string.Empty;
                var optionalOk = ValidateOptionalFields();
                if (nameError is null && optionalOk) Step = 3;
                break;
            case 3:
                if (ValidateEventFields("La date de perte est obligatoire.")) Step = 4;
                break;
            default:
                // Revalidation complète avant l'enregistrement (le brouillon n'est enregistré qu'une fois)
                if (!ValidateType()) { Step = 1; return; }
                var finalNameError = FieldRules.FullName(HolderName);
                NameError = finalNameError ?? string.Empty;
                if (finalNameError is not null || !ValidateOptionalFields()) { Step = 2; return; }
                if (!ValidateEventFields("La date de perte est obligatoire.")) { Step = 3; return; }
                OnPropertyChanged(nameof(NextText));
                try { await SubmitAsync(); }
                finally { OnPropertyChanged(nameof(NextText)); }
                break;
        }
    }

    private async Task BackAsync()
    {
        if (Step > 1) { Step--; return; }
        await LeaveAsync();
    }
}
