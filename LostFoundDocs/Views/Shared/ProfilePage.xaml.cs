using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Shared;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }

    private void OnMenuTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: ProfileMenuItem item })
        {
            _viewModel.OpenCommand.Execute(item.Route);
        }
    }
}
