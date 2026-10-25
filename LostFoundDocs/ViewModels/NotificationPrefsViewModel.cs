using LostFoundDocs.Services.Mock;

namespace LostFoundDocs.ViewModels;

/// <summary>SIMULATION : préférences gardées en mémoire (DemoSettings), sans effet sur de vraies notifications.</summary>
public sealed class NotificationPrefsViewModel : ViewModelBase
{
    public bool NotifyMatches { get => DemoSettings.NotifyMatches; set { DemoSettings.NotifyMatches = value; OnPropertyChanged(); } }
    public bool NotifyMessages { get => DemoSettings.NotifyMessages; set { DemoSettings.NotifyMessages = value; OnPropertyChanged(); } }
    public bool NotifyRestitution { get => DemoSettings.NotifyRestitution; set { DemoSettings.NotifyRestitution = value; OnPropertyChanged(); } }
    public bool NotifyReminders { get => DemoSettings.NotifyReminders; set { DemoSettings.NotifyReminders = value; OnPropertyChanged(); } }
    public bool NotifyBySms { get => DemoSettings.NotifyBySms; set { DemoSettings.NotifyBySms = value; OnPropertyChanged(); } }
}
