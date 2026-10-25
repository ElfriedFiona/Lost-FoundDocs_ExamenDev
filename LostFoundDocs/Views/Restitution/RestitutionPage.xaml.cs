using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Restitution;

public partial class RestitutionPage : ContentPage
{
    private readonly RestitutionViewModel _viewModel;

    public RestitutionPage(RestitutionViewModel viewModel)
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
