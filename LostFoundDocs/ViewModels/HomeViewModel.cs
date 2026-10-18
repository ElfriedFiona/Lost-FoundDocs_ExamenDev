using System.Collections.ObjectModel;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.Services.Validation;

namespace LostFoundDocs.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly IReportService _reports;
    private readonly INavigationService _nav;
    private readonly IConversationService _conversations;
    private readonly INotificationService _notifications;
    private string _firstName = string.Empty;
    private int _searchCount;
    private DateTime? _simulatedLastCheck;

    public HomeViewModel(IAuthService auth, IReportService reports, INavigationService nav, IConversationService conversations, INotificationService notifications)
    {
        _auth = auth;
        _reports = reports;
        _nav = nav;
        _conversations = conversations;
        _notifications = notifications;
        DeclareLostCommand = new AsyncCommand(() => _nav.GoToAsync(Routes.DeclareLost));
        DeclareFoundCommand = new AsyncCommand(() => _nav.GoToAsync(Routes.DeclareFound));
        SeeAllCommand = new AsyncCommand(() => _nav.GoToAsync("//" + Routes.Reports));
        NotificationsCommand = new AsyncCommand(() => _nav.GoToAsync(Routes.Notifications));
        OpenReportCommand = new AsyncCommand<Report>(r => r is null
            ? Task.CompletedTask
            : _nav.GoToAsync(Routes.ReportDetails, new Dictionary<string, object> { ["reportId"] = r.Id }));
    }

    public ObservableCollection<Report> ActiveReports { get; } = new();

    public ICommand DeclareLostCommand { get; }
    public ICommand DeclareFoundCommand { get; }
    public ICommand SeeAllCommand { get; }
    public ICommand NotificationsCommand { get; }
    public ICommand OpenReportCommand { get; }

    public string Greeting => $"Bonjour, {_firstName} 👋";
    public bool HasReports => ActiveReports.Count > 0;
    public bool HasNoReports => !HasReports;
    public int UnreadMessages => _conversations.Conversations.Sum(c => c.Unread);
    public bool HasUnreadNotifications => _notifications.Notifications.Any(n => !n.IsRead);

    public string SearchSummary
    {
        get
        {
            var count = _searchCount switch { 0 => "Aucune recherche active", 1 => "1 recherche active", _ => $"{_searchCount} recherches actives" };
            // SIMULATION : aucune vérification réelle n'a lieu ; l'heure est fictive.
            _simulatedLastCheck ??= Clock.Now.AddMinutes(-12);
            return _searchCount == 0 ? count : $"{count} · vérifiée {DateFormat.Ago(_simulatedLastCheck.Value, Clock.Now)}";
        }
    }

    /// <summary>Recharge l'écran à chaque affichage (le statut d'un signalement peut avoir changé).</summary>
    public void Refresh()
    {
        _firstName = _auth.CurrentUser?.FirstName ?? string.Empty;
        var all = _reports.Reports;
        _searchCount = all.Count(r => r.Status == ReportStatus.Searching);

        ActiveReports.Clear();
        foreach (var r in all.Where(r => !r.IsTerminal).OrderByDescending(r => r.UpdatedAt).Take(3))
        {
            ActiveReports.Add(r);
        }

        OnPropertyChanged(nameof(UnreadMessages));
        OnPropertyChanged(nameof(HasUnreadNotifications));
        OnPropertyChanged(nameof(Greeting));
        OnPropertyChanged(nameof(HasReports));
        OnPropertyChanged(nameof(HasNoReports));
        OnPropertyChanged(nameof(SearchSummary));
    }
}
