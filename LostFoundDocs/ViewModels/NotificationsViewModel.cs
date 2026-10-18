using System.Collections.ObjectModel;
using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class NotificationsViewModel : ViewModelBase
{
    private readonly INotificationService _notifications;
    public NotificationsViewModel(INotificationService notifications, INavigationService nav)
    {
        _notifications = notifications;
        ReadAllCommand = new Command(() => { notifications.MarkAllRead(); Refresh(); });
        DeclareCommand = new AsyncCommand(() => nav.GoToAsync(Routes.DeclareLost));
        OpenCommand = new AsyncCommand<AppNotification>(async item =>
        {
            if (item is null) return;
            item.IsRead = true;
            await nav.GoToAsync(item.Route, new Dictionary<string, object> { ["reportId"] = item.ReportId });
        });
    }
    public ObservableCollection<AppNotification> Items { get; } = new();
    public ICommand ReadAllCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand DeclareCommand { get; }
    public bool IsEmpty => Items.Count == 0;
    public bool HasUnread => Items.Any(n => !n.IsRead);
    public void Refresh()
    {
        Items.Clear();
        foreach (var item in _notifications.Notifications) Items.Add(item);
        OnPropertyChanged(string.Empty);
    }
}
