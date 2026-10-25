using System.Collections.ObjectModel;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
using LostFoundDocs.UI;

namespace LostFoundDocs.ViewModels;

/// <summary>Notification + présentation (icône, couleurs) pour l'écran.</summary>
public sealed class NotificationRow
{
    public NotificationRow(AppNotification notification)
    {
        Notification = notification;
        (Glyph, Background, Foreground) = notification.Kind switch
        {
            NotificationKind.Match => (Icons.ScanSearch, Res.GetColor("Primary"), Res.GetColor("White")),
            NotificationKind.Message => (Icons.MessageSquare, Res.GetColor("PrimarySoft"), Res.GetColor("Primary")),
            NotificationKind.Restitution => (Icons.Check, Res.GetColor("SuccessSoft"), Res.GetColor("SuccessText")),
            _ => (Icons.Bell, Res.GetColor("NeutralSoft"), Res.GetColor("TextSecondary")),
        };
    }

    public AppNotification Notification { get; }
    public string Glyph { get; }
    public Color Background { get; }
    public Color Foreground { get; }
}

/// <summary>SIMULATION : notifications générées localement (aucune notification push réelle).</summary>
public sealed class NotificationsViewModel : ViewModelBase
{
    private readonly INotificationService _notifications;
    private readonly IRestitutionService _restitution;
    private readonly INavigationService _nav;

    public NotificationsViewModel(INotificationService notifications, IRestitutionService restitution, INavigationService nav)
    {
        _notifications = notifications;
        _restitution = restitution;
        _nav = nav;
        MarkAllReadCommand = new AsyncCommand(async () => { await _notifications.MarkAllReadAsync(); Refresh(); }, () => HasUnread);
        OpenCommand = new AsyncCommand<NotificationRow>(OpenAsync);
    }

    public ObservableCollection<NotificationRow> Today { get; } = new();
    public ObservableCollection<NotificationRow> Earlier { get; } = new();
    public ICommand MarkAllReadCommand { get; }
    public ICommand OpenCommand { get; }

    public bool HasToday => Today.Count > 0;
    public bool HasEarlier => Earlier.Count > 0;
    public bool IsEmpty => !HasToday && !HasEarlier;
    public bool HasUnread => _notifications.UnreadCount > 0;

    public void Refresh()
    {
        Today.Clear();
        Earlier.Clear();
        foreach (var n in _notifications.Notifications)
            (n.IsToday ? Today : Earlier).Add(new NotificationRow(n));
        RaiseAll();
        (MarkAllReadCommand as AsyncCommand)?.RaiseCanExecuteChanged();
    }

    private async Task OpenAsync(NotificationRow? row)
    {
        if (row is null) return;
        var n = row.Notification;
        await _notifications.MarkReadAsync(n.Id);
        row.Notification.IsRead = true;
        OnPropertyChanged(nameof(HasUnread));

        var reportParams = n.ReportId is { } r ? new Dictionary<string, object> { ["reportId"] = r } : null;
        switch (n.Kind)
        {
            case NotificationKind.Message when n.ConversationId is { } c:
                await _nav.GoToAsync(Routes.Conversation, new Dictionary<string, object> { ["conversationId"] = c });
                break;
            case NotificationKind.Match when reportParams is not null:
                await _nav.GoToAsync(Routes.MatchDetails, reportParams);
                break;
            case NotificationKind.Restitution when reportParams is not null && n.ReportId is { } id && _restitution.GetReceipt(id) is not null:
                await _nav.GoToAsync(Routes.Receipt, reportParams);
                break;
            default:
                if (reportParams is not null) await _nav.GoToAsync(Routes.ReportDetails, reportParams);
                break;
        }
    }
}
