using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Mock;
using LostFoundDocs.UI;

namespace LostFoundDocs.ViewModels;

/// <summary>À propos + « Mode démonstration » : règle de correspondance, réponse automatique, réinitialisation des données fictives.</summary>
public sealed class AboutViewModel : ViewModelBase
{
    private readonly IEnumerable<IResettable> _resettables;
    private readonly IDialogService _dialogs;
    private OptionItem? _selectedMode;

    public AboutViewModel(IEnumerable<IResettable> resettables, IDialogService dialogs)
    {
        _resettables = resettables;
        _dialogs = dialogs;
        _selectedMode = ModeOptions.First(o => o.Id == DemoSettings.MatchMode.ToString());
        ResetCommand = new AsyncCommand(ResetAsync);
    }

    public ICommand ResetCommand { get; }
    public string Version => "Version 1.0 · frontend de démonstration";

    public IList<OptionItem> ModeOptions { get; } = new List<OptionItem>
    {
        new(nameof(MatchMode.Auto), "Automatique", "Une correspondance existe pour certains types à Douala (diplôme, CNI, permis, passeport, carte d’étudiant, carte de contribuable)."),
        new(nameof(MatchMode.AlwaysMatch), "Toujours une correspondance", "Pour tester le parcours complet quel que soit le document."),
        new(nameof(MatchMode.NeverMatch), "Jamais de correspondance", "Pour tester l’écran « Aucune correspondance »."),
    };

    public OptionItem? SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (!SetProperty(ref _selectedMode, value) || value is null) return;
            DemoSettings.MatchMode = Enum.Parse<MatchMode>(value.Id);
        }
    }

    public bool AutoReply
    {
        get => DemoSettings.AutoReply;
        set { DemoSettings.AutoReply = value; OnPropertyChanged(); }
    }

    private async Task ResetAsync()
    {
        var ok = await _dialogs.ConfirmAsync("Réinitialiser les données ?",
            "Les signalements, conversations et notifications créés pendant la démonstration seront effacés.", "Réinitialiser");
        if (!ok) return;
        foreach (var r in _resettables) r.Reset();
        await _dialogs.AlertAsync("Données réinitialisées", "Le jeu de données de démonstration d’origine est rétabli.");
    }
}
