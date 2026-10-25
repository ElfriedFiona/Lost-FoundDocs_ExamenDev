using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;

namespace LostFoundDocs.ViewModels;

/// <summary>Écran « Document restitué » (confirmation de la remise).</summary>
public sealed class RestitutedViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IRestitutionService _restitution;
    private readonly INavigationService _nav;
    private Guid _id;
    private Receipt? _receipt;

    public RestitutedViewModel(IRestitutionService restitution, INavigationService nav)
    {
        _restitution = restitution;
        _nav = nav;
        ProofCommand = new AsyncCommand(() => _nav.GoToAsync("../" + Routes.Receipt, new Dictionary<string, object> { ["reportId"] = _id }));
        HomeCommand = new AsyncCommand(() => _nav.GoToAsync("//" + Routes.Home));
    }

    public ICommand ProofCommand { get; }
    public ICommand HomeCommand { get; }

    public bool HasReceipt => _receipt is not null;
    public string DocumentTitle => _receipt?.DocumentTitle ?? string.Empty;
    public string DateLabel => _receipt?.DateLabel ?? string.Empty;
    public string OwnerName => _receipt?.OwnerName ?? string.Empty;
    public string HandedByName => _receipt?.HandedByName ?? string.Empty;
    public string Reference => _receipt?.Reference ?? string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id)
        {
            _id = id;
            _receipt = _restitution.GetReceipt(id);
            RaiseAll();
        }
    }
}

/// <summary>Écran « Preuve de restitution ». SIMULATION : reçu fictif, non vérifiable ; l'enregistrement du PDF est simulé.</summary>
public sealed class ReceiptViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IRestitutionService _restitution;
    private Receipt? _receipt;
    private bool _saved;

    public ReceiptViewModel(IRestitutionService restitution)
    {
        _restitution = restitution;
        SaveCommand = new Command(() => { Saved = true; }, () => !Saved);
    }

    public ICommand SaveCommand { get; }

    public bool HasReceipt => _receipt is not null;
    public bool HasNoReceipt => !HasReceipt;
    public string Reference => _receipt?.Reference ?? string.Empty;
    public string DocumentTitle => _receipt?.DocumentTitle ?? string.Empty;
    public string DateLabel => _receipt is null ? string.Empty : _receipt.DateLabel;
    public string City => _receipt?.City ?? string.Empty;
    public string OwnerName => _receipt?.OwnerName ?? string.Empty;
    public string HandedByName => _receipt?.HandedByName ?? string.Empty;
    public string MatchLabel => _receipt?.MatchLabel ?? string.Empty;
    public string VerificationCode => _receipt?.VerificationCode ?? string.Empty;
    public string QrPayload => _receipt is null ? string.Empty : $"{_receipt.Reference}|{_receipt.VerificationCode}";

    public bool Saved
    {
        get => _saved;
        private set
        {
            if (!SetProperty(ref _saved, value)) return;
            OnPropertyChanged(nameof(SaveText));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(SaveGlyph));
            (SaveCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool CanSave => !Saved;
    public string SaveText => Saved ? "Reçu enregistré" : "Télécharger le reçu (PDF)";
    public string SaveGlyph => Saved ? UI.Icons.Check : UI.Icons.Download;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id)
        {
            _receipt = _restitution.GetReceipt(id);
            RaiseAll();
        }
    }
}
