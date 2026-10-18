using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class RestitutionViewModel : ReportContextViewModel
{
    private readonly IRestitutionService _restitution;
    private readonly IDialogService _dialogs;
    private DateTime _day = Clock.Today;
    private string? _slot;
    private bool _editing;
    public RestitutionProposal? Proposal { get; private set; }
    public RestitutionViewModel(IReportService reports, INavigationService nav, IRestitutionService restitution, IDialogService dialogs) : base(reports, nav)
    {
        _restitution = restitution; _dialogs = dialogs;
        ProposeCommand = new AsyncCommand(ProposeAsync);
        AcceptCommand = new AsyncCommand(async () => { await restitution.AcceptAsync(ReportId); await Reload(); });
        ChangeCommand = new Command(() => { _editing = true; OnPropertyChanged(string.Empty); });
        ConfirmCommand = new AsyncCommand(ConfirmAsync);
        ConversationCommand = new AsyncCommand(() => OpenAsync(Routes.Conversation));
    }
    public string[] Places { get; } = ["Commissariat du 11e arrondissement – Bonamoussadi", "Hôtel de ville de Douala 1er – Bonanjo", "Entrée principale, Université de Douala – Campus 1"];
    public DateTime[] Days => [Clock.Today, Clock.Today.AddDays(1), Clock.Today.AddDays(2)];
    public string[] Slots => new[] { "09:00", "10:00", "14:00", "16:00" }.Where(s => Day.Add(TimeSpan.Parse(s)) > Clock.Now).ToArray();
    public string? Place { get; set; }
    public DateTime Day { get => _day; set { _day = value; Slot = null; OnPropertyChanged(nameof(Slots)); } }
    public string? Slot { get => _slot; set => SetProperty(ref _slot, value); }
    public string Error { get; private set; } = "";
    public bool ShowForm => Proposal is null || _editing;
    public bool HasProposal => Proposal is not null && !_editing;
    public bool CanAccept => HasProposal && Proposal is { Accepted: false } && !Expired;
    public bool CanConfirm => HasProposal && Proposal is { Accepted: true };
    public bool Expired => Proposal?.Appointment <= Clock.Now;
    public string Title => Report?.Kind == ReportKind.Lost ? "Proposition reçue" : "Organiser la restitution";
    public string ConfirmLabel => Report?.Kind == ReportKind.Lost ? "J’ai récupéré mon document" : "Confirmer la restitution";
    public ICommand ProposeCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand ChangeCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand ConversationCommand { get; }
    protected override async Task LoadAsync() => Proposal = await _restitution.GetProposalAsync(ReportId);
    private async Task Reload() { await LoadAsync(); OnPropertyChanged(string.Empty); }
    private async Task ProposeAsync()
    {
        Error = "";
        if (Place is null || Slot is null) { Error = "Choisissez un lieu et un créneau disponible."; OnPropertyChanged(nameof(Error)); return; }
        await _restitution.ProposeAsync(ReportId, Place, Day.Add(TimeSpan.Parse(Slot)));
        _editing = false;
        await Reload();
    }
    private async Task ConfirmAsync()
    {
        var text = Report?.Kind == ReportKind.Lost ? "Confirmez-vous avoir récupéré votre document ?" : "Le document a-t-il effectivement été remis à son propriétaire ?";
        if (!await _dialogs.ConfirmAsync("Confirmer la restitution ?", text, "Confirmer")) return;
        await _restitution.ConfirmAsync(ReportId);
        await OpenAsync(Routes.Returned);
    }
}
