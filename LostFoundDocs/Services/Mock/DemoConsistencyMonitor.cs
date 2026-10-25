using System.Diagnostics;
using LostFoundDocs.Services.Abstractions;

namespace LostFoundDocs.Services.Mock;

/// <summary>
/// Surveille la cohérence chronologique des données fictives PENDANT l'utilisation (Debug uniquement) :
/// après chaque changement de signalement, de conversation ou de notification, les validateurs sont relancés
/// et toute incohérence (date future, restitution avant correspondance, doublon de référence…) est écrite dans
/// la fenêtre Sortie de Visual Studio. Aucun effet en Release.
/// </summary>
public sealed class DemoConsistencyMonitor
{
    private readonly IReportService _reports;
    private readonly DemoWorld _world;
    private string _lastReport = string.Empty;

    public DemoConsistencyMonitor(IReportService reports, DemoWorld world, IConversationService conversations, INotificationService notifications)
    {
        _reports = reports;
        _world = world;
        _reports.ReportsChanged += (_, _) => Check("signalements");
        conversations.Changed += (_, _) => Check("conversations");
        notifications.Changed += (_, _) => Check("notifications");
    }

    [Conditional("DEBUG")]
    private void Check(string source)
    {
        try
        {
            List<string> problems;
            lock (_world.Gate)
            {
                var reports = _reports.Reports;
                problems = DemoDataValidator.Validate(reports, Clock.Now)
                    .Concat(DemoWorldValidator.Validate(_world.Current, reports, Clock.Now))
                    .ToList();
            }

            var report = problems.Count == 0
                ? $"[Cohérence] OK après modification des {source}"
                : $"[Cohérence] INCOHÉRENCES après modification des {source} : " + string.Join(" | ", problems);
            if (report != _lastReport)
            {
                Debug.WriteLine(report);
                _lastReport = report;
            }
        }
        catch (Exception ex)
        {
            // Une collection peut changer pendant la lecture (réponse simulée) : on ignore, le prochain événement relancera le contrôle.
            Debug.WriteLine($"[Cohérence] contrôle ignoré : {ex.Message}");
        }
    }
}
