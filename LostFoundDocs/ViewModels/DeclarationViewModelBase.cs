using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

/// <summary>Socle commun des formulaires « perdu » et « trouvé » : champs, libellés dynamiques selon le type, validation.</summary>
public abstract class DeclarationViewModelBase : ViewModelBase
{
    protected readonly IReportService Reports;
    protected readonly INavigationService Nav;
    protected readonly IDialogService Dialogs;
    protected readonly ReportDraft Draft;

    private string? _selectedTypeLabel;
    private string _holderName = string.Empty, _customTitle = string.Empty, _number = string.Empty, _issuer = string.Empty;
    private string _city = "Douala", _place = string.Empty, _notes = string.Empty;
    private DateTime? _birthDate, _eventDate;
    private string? _photoName;
    private string _typeError = string.Empty, _nameError = string.Empty, _customTitleError = string.Empty;
    private string _birthError = string.Empty, _numberError = string.Empty, _dateError = string.Empty;
    private string _placeError = string.Empty, _notesError = string.Empty, _formError = string.Empty;

    protected DeclarationViewModelBase(IReportService reports, INavigationService nav, IDialogService dialogs, ReportKind kind)
    {
        Reports = reports;
        Nav = nav;
        Dialogs = dialogs;
        Draft = new ReportDraft { Kind = kind };
        PickPhotoCommand = new AsyncCommand(PickPhotoAsync);
        RemovePhotoCommand = new Command(() => PhotoName = null);
        PlaceChipCommand = new Command<string>(p => Place = p ?? string.Empty);
    }

    public IList<string> TypeLabels { get; } = DocumentTypes.All.Select(t => t.Label).ToList();
    public IList<string> CityOptions { get; } = new List<string> { "Douala", "Yaoundé", "Bafoussam", "Garoua", "Limbé", "Kribi", "Autre ville" };
    public IList<string> QuickPlaces { get; } = new List<string> { "Université de Douala", "Akwa", "Bonamoussadi", "Ndokoti", "Bonabéri", "Deido" };

    public ICommand PickPhotoCommand { get; }
    public ICommand RemovePhotoCommand { get; }
    public ICommand PlaceChipCommand { get; }

    // ----- champs -----
    public string? SelectedTypeLabel
    {
        get => _selectedTypeLabel;
        set
        {
            if (!SetProperty(ref _selectedTypeLabel, value)) return;
            TypeError = string.Empty;
            foreach (var n in new[] { nameof(TypeInfo), nameof(HasType), nameof(IsOtherType), nameof(NumberLabel), nameof(NumberBadge),
                                      nameof(IssuerLabel), nameof(IssuerPlaceholder), nameof(TypeHint), nameof(SummaryDocument) })
                OnPropertyChanged(n);
        }
    }

    public string HolderName { get => _holderName; set { if (SetProperty(ref _holderName, value)) { NameError = string.Empty; OnPropertyChanged(nameof(SummaryName)); } } }
    public string CustomTitle { get => _customTitle; set { if (SetProperty(ref _customTitle, value)) CustomTitleError = string.Empty; } }
    public DateTime? BirthDate { get => _birthDate; set { if (SetProperty(ref _birthDate, value)) BirthError = string.Empty; } }
    public string Number { get => _number; set { if (SetProperty(ref _number, value)) NumberError = string.Empty; } }
    public string Issuer { get => _issuer; set => SetProperty(ref _issuer, value); }
    public DateTime? EventDate { get => _eventDate; set { if (SetProperty(ref _eventDate, value)) { DateError = string.Empty; OnPropertyChanged(nameof(SummaryEvent)); } } }
    public string City { get => _city; set { if (SetProperty(ref _city, value ?? string.Empty)) OnPropertyChanged(nameof(SummaryEvent)); } }
    public string Place { get => _place; set { if (SetProperty(ref _place, value)) { PlaceError = string.Empty; OnPropertyChanged(nameof(SummaryEvent)); } } }
    public string Notes { get => _notes; set { if (SetProperty(ref _notes, value)) NotesError = string.Empty; } }

    public string? PhotoName
    {
        get => _photoName;
        set
        {
            if (SetProperty(ref _photoName, value))
            {
                OnPropertyChanged(nameof(HasPhoto));
                OnPropertyChanged(nameof(HasNoPhoto));
            }
        }
    }

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoName);
    public bool HasNoPhoto => !HasPhoto;

    // ----- erreurs -----
    public string TypeError { get => _typeError; set => SetProperty(ref _typeError, value); }
    public string NameError { get => _nameError; set => SetProperty(ref _nameError, value); }
    public string CustomTitleError { get => _customTitleError; set => SetProperty(ref _customTitleError, value); }
    public string BirthError { get => _birthError; set => SetProperty(ref _birthError, value); }
    public string NumberError { get => _numberError; set => SetProperty(ref _numberError, value); }
    public string DateError { get => _dateError; set => SetProperty(ref _dateError, value); }
    public string PlaceError { get => _placeError; set => SetProperty(ref _placeError, value); }
    public string NotesError { get => _notesError; set => SetProperty(ref _notesError, value); }
    public string FormError
    {
        get => _formError;
        set { if (SetProperty(ref _formError, value)) OnPropertyChanged(nameof(HasFormError)); }
    }
    public bool HasFormError => !string.IsNullOrEmpty(FormError);

    // ----- libellés dynamiques selon le type -----
    public DocumentTypeInfo? TypeInfo => DocumentTypes.All.FirstOrDefault(t => t.Label == SelectedTypeLabel);
    public bool HasType => TypeInfo is not null;
    public bool IsOtherType => TypeInfo?.Type == DocumentType.Other;
    public string NumberLabel => TypeInfo?.NumberLabel ?? "Numéro du document";
    public string NumberBadge => TypeInfo?.NumberRecommended == true ? "Recommandé" : string.Empty;
    public string NumberPlaceholder => "Ex. 112048213";
    public string IssuerLabel => TypeInfo?.IssuerLabel ?? "Établissement / organisme de délivrance";
    public string IssuerPlaceholder => TypeInfo?.IssuerExample ?? "Ex. Université de Douala";
    public string SummaryDocument => TypeInfo is null ? string.Empty : (IsOtherType && !string.IsNullOrWhiteSpace(CustomTitle) ? CustomTitle : TypeInfo.Label);
    public string SummaryName => string.IsNullOrWhiteSpace(HolderName) ? "—" : NameFormat.Capitalize(HolderName);
    public string SummaryEvent
    {
        get
        {
            var date = EventDate is { } d ? DateFormat.Short(d) : "date à préciser";
            var place = string.IsNullOrWhiteSpace(Place) ? City : $"{Place}, {City}";
            return $"{date} · {place}";
        }
    }
    public virtual string TypeHint => string.Empty;

    /// <summary>Vrai si l'utilisateur a saisi quelque chose (déclenche la modale d'abandon).</summary>
    protected bool IsDirty =>
        HasType || !string.IsNullOrWhiteSpace(HolderName) || !string.IsNullOrWhiteSpace(Number) ||
        !string.IsNullOrWhiteSpace(Issuer) || BirthDate is not null || EventDate is not null ||
        !string.IsNullOrWhiteSpace(Place) || !string.IsNullOrWhiteSpace(Notes) || HasPhoto;

    // ----- validations réutilisables -----
    protected bool ValidateType()
    {
        TypeError = HasType ? string.Empty : "Sélectionnez un type de document.";
        return HasType;
    }

    protected bool ValidateOptionalFields()
    {
        BirthError = FieldRules.OptionalBirthDate(BirthDate, Clock.Today) ?? string.Empty;
        NumberError = FieldRules.OptionalDocumentNumber(Number) ?? string.Empty;
        CustomTitleError = IsOtherType && string.IsNullOrWhiteSpace(CustomTitle) ? "Indiquez l’intitulé du document." : string.Empty;
        return BirthError.Length == 0 && NumberError.Length == 0 && CustomTitleError.Length == 0;
    }

    protected bool ValidateEventFields(string dateRequiredMessage)
    {
        DateError = FieldRules.EventDate(EventDate, Clock.Today, BirthDate, dateRequiredMessage) ?? string.Empty;
        PlaceError = FieldRules.Required(Place, "Indiquez le lieu, même approximatif.") ?? string.Empty;
        NotesError = FieldRules.Notes(Notes) ?? string.Empty;
        return DateError.Length == 0 && PlaceError.Length == 0 && NotesError.Length == 0;
    }

    protected void SyncDraft()
    {
        Draft.Type = TypeInfo?.Type;
        Draft.CustomTitle = IsOtherType ? CustomTitle.Trim() : null;
        Draft.HolderName = HolderName.Trim();
        Draft.Number = string.IsNullOrWhiteSpace(Number) ? null : Number.Trim();
        Draft.BirthDate = BirthDate;
        Draft.Issuer = string.IsNullOrWhiteSpace(Issuer) ? null : Issuer.Trim();
        Draft.EventDate = EventDate;
        Draft.City = City;
        Draft.Place = Place.Trim();
        Draft.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        Draft.PhotoPath = PhotoName;
    }

    /// <summary>Enregistre le brouillon (une seule fois, idempotent) puis ouvre l'écran de confirmation à la place du formulaire.</summary>
    protected async Task SubmitAsync()
    {
        SyncDraft();
        IsBusy = true;
        try
        {
            var report = await Reports.SubmitAsync(Draft);
            await Nav.GoToAsync("../" + Routes.DeclarationConfirmation, new Dictionary<string, object> { ["reportId"] = report.Id });
        }
        catch (ArgumentException ex)
        {
            FormError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task LeaveAsync()
    {
        if (IsDirty)
        {
            var leave = await Dialogs.ConfirmAsync("Abandonner la déclaration ?",
                "Les informations saisies seront perdues.", "Abandonner", "Continuer");
            if (!leave) return;
        }
        await Nav.GoBackAsync();
    }

    // SIMULATION (frontend uniquement) : aucun accès à l'appareil photo ni à la galerie.
    private async Task PickPhotoAsync()
    {
        var choice = await Dialogs.ChooseAsync("Ajouter une photo du document", "Annuler", "Prendre une photo", "Choisir dans la galerie");
        if (choice is null) return;
        PhotoName = choice.StartsWith("Prendre")
            ? $"photo_{Clock.Now:yyyyMMdd_HHmm}.jpg"
            : "document_galerie.png";
    }
}
