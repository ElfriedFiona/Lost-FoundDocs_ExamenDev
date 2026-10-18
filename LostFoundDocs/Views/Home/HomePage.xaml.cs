using LostFoundDocs.Models;
using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
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
