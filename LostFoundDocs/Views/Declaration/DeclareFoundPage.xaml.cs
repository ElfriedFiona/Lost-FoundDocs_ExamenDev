using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Declaration;

public partial class DeclareFoundPage : ContentPage
{
    private readonly DeclareFoundViewModel _viewModel;

    public DeclareFoundPage(DeclareFoundViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }
}
