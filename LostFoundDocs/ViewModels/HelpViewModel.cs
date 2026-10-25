using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.UI;

namespace LostFoundDocs.ViewModels;

public sealed class FaqItem : ObservableObject
{
    private bool _isOpen;
    public FaqItem(string question, string answer) { Question = question; Answer = answer; }
    public string Question { get; }
    public string Answer { get; }
    public bool IsOpen { get => _isOpen; set { if (SetProperty(ref _isOpen, value)) OnPropertyChanged(nameof(Glyph)); } }
    public string Glyph => IsOpen ? Icons.ChevronDown : Icons.ChevronRight;
}

public sealed class HelpViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    public HelpViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
        ToggleCommand = new Command<FaqItem>(i => { if (i is not null) i.IsOpen = !i.IsOpen; });
        ContactCommand = new AsyncCommand(() => _dialogs.AlertAsync("Support (simulation)",
            "Aucun message n’a été envoyé : le support réel sera disponible avec le backend."));
    }

    public ICommand ToggleCommand { get; }
    public ICommand ContactCommand { get; }

    public IList<FaqItem> Faq { get; } = new List<FaqItem>
    {
        new("Comment signaler un document perdu ?", "Depuis l’accueil, touchez « J’ai perdu un document », renseignez le type, l’identité, les circonstances puis validez. La recherche d’une correspondance démarre ensuite."),
        new("Comment signaler un document trouvé ?", "Touchez « J’ai trouvé un document » et renseignez uniquement ce qui est visible. Un nom ou un numéro suffit ; un numéro inconnu n’empêche pas de continuer."),
        new("Mes informations sont-elles visibles ?", "Non. Les numéros de documents sont masqués (seuls les 4 derniers caractères s’affichent), la date de naissance n’est jamais affichée en entier et les photos ne sont pas publiques."),
        new("Comment se passe la restitution ?", "Après une correspondance, la messagerie s’ouvre. Convenez d’un lieu public et d’un créneau, puis confirmez la remise : un reçu est généré."),
        new("Dois-je payer quelque chose ?", "Non. Aucune somme d’argent ne doit être demandée lors d’une restitution. Signalez toute demande de ce type."),
        new("Cette application est-elle connectée à un vrai service ?", "Pas encore : cette version est une démonstration. Les données sont fictives et locales ; le backend sera développé ultérieurement."),
    };
}
