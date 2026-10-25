using LostFoundDocs.Models;
using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Shared;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;

    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }

    private void OnReportTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: Report report })
        {
            _viewModel.OpenReportCommand.Execute(report);
        }
    }
}
