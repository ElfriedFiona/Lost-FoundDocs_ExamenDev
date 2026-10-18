using System.ComponentModel;
using LostFoundDocs.Models;
using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>Carte de signalement (BindingContext = Report). Le statut se met à jour tout seul quand il évolue.</summary>
public partial class ReportCard : ContentView
{
    public static readonly BindableProperty ShowLocationProperty =
        BindableProperty.Create(nameof(ShowLocation), typeof(bool), typeof(ReportCard), true,
            propertyChanged: (b, _, _) => ((ReportCard)b).ApplyReport());

    private Report? _report;

    public ReportCard() => InitializeComponent();

    public event EventHandler? Tapped;

    public bool ShowLocation
    {
        get => (bool)GetValue(ShowLocationProperty);
        set => SetValue(ShowLocationProperty, value);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_report is not null) _report.PropertyChanged -= OnReportChanged;
        _report = BindingContext as Report;
        if (_report is not null) _report.PropertyChanged += OnReportChanged;
        ApplyReport();
    }

    private void OnReportChanged(object? sender, PropertyChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(ApplyReport);

    private void ApplyReport()
    {
        if (_report is null) return;
        IconView.Text = Presentation.Icon(_report.Type);
        Badge.Text = _report.StatusLabel;
        Badge.Kind = Presentation.Kind(_report.Status);
        Badge.ShowCheck = _report.Status == ReportStatus.Returned;
        LocationRow.IsVisible = ShowLocation && !string.IsNullOrWhiteSpace(_report.LocationLabel);
        SemanticProperties.SetDescription(this, $"{_report.Title}, {_report.DateLabel}, {_report.StatusLabel}");
        SemanticProperties.SetHint(this, "Ouvrir le détail du signalement");
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Tapped?.Invoke(this, EventArgs.Empty);
}
