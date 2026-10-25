using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Profile;

public partial class HelpPage : ContentPage
{
    private readonly HelpViewModel _viewModel;

    public HelpPage(HelpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private void OnFaqTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: FaqItem item })
        {
            _viewModel.ToggleCommand.Execute(item);
        }
    }
}
