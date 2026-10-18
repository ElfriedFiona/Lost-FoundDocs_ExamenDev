using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class ReceiptViewModel : ReportContextViewModel
{
    private readonly IRestitutionService _restitution;
    public Receipt? Receipt { get; private set; }
    public ReceiptViewModel(IReportService reports, INavigationService nav, IRestitutionService restitution, IDialogService dialogs) : base(reports, nav)
    {
        _restitution = restitution;
        HomeCommand = new AsyncCommand(() => nav.GoToAsync("//" + Routes.Home));
        ProofCommand = new AsyncCommand(() => OpenAsync(Routes.Receipt));
        DownloadCommand = new AsyncCommand(async () =>
        {
            if (Receipt is null) return;
            await dialogs.AlertAsync("Enregistrement simulé", "Aucun PDF réel n’est téléchargé. Ce reçu de démonstration est non vérifiable.");
            Receipt.Saved = true;
            OnPropertyChanged(string.Empty);
        });
    }
    public bool CanDownload => Receipt is { Saved: false };
    public string DownloadLabel => CanDownload ? "Télécharger le reçu (simulation)" : "Reçu enregistré (simulation)";
    public string VerificationCode => Report is null ? "" : "DÉMO-" + Report.Reference;
    public ICommand HomeCommand { get; }
    public ICommand ProofCommand { get; }
    public ICommand DownloadCommand { get; }
    protected override async Task LoadAsync() => Receipt = await _restitution.GetReceiptAsync(ReportId);
}
