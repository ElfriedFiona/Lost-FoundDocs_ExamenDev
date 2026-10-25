using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Reports;

public partial class ReportDetailsPage : ContentPage
{
    private readonly ReportDetailsViewModel _viewModel;

    public ReportDetailsPage(ReportDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
