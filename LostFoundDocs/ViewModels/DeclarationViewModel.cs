using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;
namespace LostFoundDocs.ViewModels;

public sealed class DeclarationViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IReportService _reports;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly ReportDraft _draft = new();
    private DocumentTypeInfo? _selectedType;
    private int _step = 1;
    private bool _hasBirthDate;
    private DateTime _birthDate = Clock.Today.AddYears(-20);
    private Dictionary<string, string> _errors = new[] { "Type", "CustomTitle", "Name", "Number", "Birth", "Date", "City", "Place", "Notes" }.ToDictionary(key => key, _ => "");
    public event Action<string>? FocusRequested;

    public DeclarationViewModel(IReportService reports, INavigationService nav, IDialogService dialogs)
    {
        _reports = reports; _nav = nav; _dialogs = dialogs;
        _draft.EventDate = Clock.Today;
        ContinueCommand = new AsyncCommand(ContinueAsync);
        BackCommand = new AsyncCommand(BackAsync);
        CloseCommand = new AsyncCommand(CloseAsync);
        EditCommand = new Command(() => { _step = 1; Refresh(); });
        PhotoCommand = new AsyncCommand(PhotoAsync);
        RemovePhotoCommand = new Command(() => { _draft.PhotoPath = null; Refresh(); });
        PlaceCommand = new Command<string>(place => { Place = place; ValidateCurrent(); Refresh(); });
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _draft.Kind = query.TryGetValue("kind", out var kind) && kind is ReportKind value ? value : ReportKind.Lost;
        Refresh();
    }
    public ICommand ContinueCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand PhotoCommand { get; }
    public ICommand RemovePhotoCommand { get; }
    public ICommand PlaceCommand { get; }
    public IReadOnlyList<DocumentTypeInfo> Types => DocumentTypes.All;
    public string[] Cities { get; } = ["Douala", "Yaoundé", "Garoua", "Limbé", "Bertoua", "Autre ville"];
    public string[] Places { get; } = ["Université de Douala", "Akwa", "Bonamoussadi", "Ndokoti", "Bonabéri", "Deido"];
    public bool IsFound => _draft.Kind == ReportKind.Found;
    public bool IsLost => !IsFound;
    public string Title => IsFound ? "Document trouvé" : "Document perdu";
    public string StepLabel => IsFound ? "Informations visibles sur le document" : $"Étape {_step} sur 4";
    public double Progress => IsFound ? 1 : _step / 4d;
    public bool ShowType => IsFound || _step == 1;
    public bool ShowIdentity => IsFound || _step == 2;
    public bool ShowCircumstances => IsFound || _step == 3;
    public bool ShowReview => IsFound || _step == 4;
    public bool IsOther => SelectedType?.Type == DocumentType.Other;
    public string ActionText => IsFound || _step == 4 ? "Déclarer le document" : "Continuer";
    public string NumberLabel => SelectedType?.NumberLabel ?? "Numéro du document";
    public string IssuerLabel => SelectedType?.IssuerLabel ?? "Organisme de délivrance";
    public string NumberBadge => SelectedType?.NumberRecommended == true ? "Recommandé" : "Facultatif";
    public DateTime Today => Clock.Today;
    public Dictionary<string, string> Errors => _errors;
    public string Summary => $"{SelectedType?.Label}\n{_draft.HolderName}\nNuméro : {Masking.MaskNumber(_draft.Number)}\n{_draft.EventDate:dd/MM/yyyy} · {_draft.City}\n{_draft.Place}";
    public bool HasPhoto => _draft.PhotoPath is not null;
    public string PhotoLabel => HasPhoto ? "Photo de démonstration ajoutée (privée)" : "Ajouter une photo (facultatif)";
    public string? PhotoPath => _draft.PhotoPath;
    public DocumentTypeInfo? SelectedType
    {
        get => _selectedType;
        set
        {
            if (!SetProperty(ref _selectedType, value)) return;
            _draft.Type = value?.Type;
            if (_errors.Values.Any(error => error.Length > 0)) ValidateCurrent();
            Refresh();
        }
    }
    public string CustomTitle { get => _draft.CustomTitle ?? ""; set => _draft.CustomTitle = value; }
    public string Name { get => _draft.HolderName; set => _draft.HolderName = value; }
    public string Number { get => _draft.Number ?? ""; set => _draft.Number = value; }
    public string Issuer { get => _draft.Issuer ?? ""; set => _draft.Issuer = value; }
    public string City { get => _draft.City; set => _draft.City = value; }
    public string Place { get => _draft.Place; set { _draft.Place = value; OnPropertyChanged(); } }
    public string Notes { get => _draft.Notes ?? ""; set => _draft.Notes = value; }
    public DateTime EventDate { get => _draft.EventDate ?? Today; set => _draft.EventDate = value; }
    public DateTime BirthDate { get => _birthDate; set { _birthDate = value; _draft.BirthDate = HasBirthDate ? value : null; } }
    public bool HasBirthDate { get => _hasBirthDate; set { SetProperty(ref _hasBirthDate, value); _draft.BirthDate = value ? _birthDate : null; } }
    public void ValidateCurrent()
    {
        _errors = DeclarationRules.Validate(_draft, IsFound ? 0 : _step, Today);
        // Indexer bindings need a value for each field, including fields without errors.
        foreach (var key in new[] { "Type", "CustomTitle", "Name", "Number", "Birth", "Date", "City", "Place", "Notes" })
            _errors.TryAdd(key, "");
        OnPropertyChanged(nameof(Errors));
    }
    private async Task ContinueAsync()
    {
        ValidateCurrent();
        var invalid = _errors.FirstOrDefault(e => e.Value.Length > 0);
        if (invalid.Key is not null) { FocusRequested?.Invoke(invalid.Key); return; }
        if (!IsFound && _step < 4) { _step++; Refresh(); return; }
        IsBusy = true;
        try
        {
            var report = await _reports.SubmitAsync(_draft);
            await _nav.GoToAsync(Routes.Confirmation, new Dictionary<string, object> { ["reportId"] = report.Id });
        }
        finally { IsBusy = false; }
    }
    private Task BackAsync()
    {
        if (IsBusy) return Task.CompletedTask;
        if (!IsFound && _step > 1) { _step--; Refresh(); return Task.CompletedTask; }
        return CloseAsync();
    }
    public async Task<bool> CanLeaveAsync()
    {
        if (IsBusy) return false;
        if (_draft.SubmittedReportId is not null) return true;
        var dirty = _draft.Type is not null || Name.Length > 0 || Number.Length > 0 || Place.Length > 0 || Notes.Length > 0 || HasPhoto;
        return !dirty || await _dialogs.ConfirmAsync("Abandonner la déclaration ?", "Les informations non enregistrées seront perdues.", "Abandonner", "Continuer");
    }
    private async Task CloseAsync() { if (await CanLeaveAsync()) await _nav.GoToAsync("//" + Routes.Home); }
    private async Task PhotoAsync()
    {
        var choice = await _dialogs.ChooseAsync("Photo simulée · JPG/PNG · 10 Mo max", "Annuler",
            "Prendre une photo", "Choisir dans la galerie", "Simuler un fichier invalide");
        if (choice is null or "Annuler") return;
        if (choice == "Simuler un fichier invalide")
            await _dialogs.AlertAsync("Photo refusée", "Utilisez une image JPG ou PNG de 10 Mo maximum.");
        else { _draft.PhotoPath = "document_demo.png"; Refresh(); }
    }
    private void Refresh() => OnPropertyChanged(string.Empty);
}
