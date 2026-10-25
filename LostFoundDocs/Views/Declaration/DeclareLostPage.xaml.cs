using LostFoundDocs.ViewModels;

namespace LostFoundDocs.Views.Declaration;

public partial class DeclareLostPage : ContentPage
{
    private readonly DeclareLostViewModel _viewModel;

    public DeclareLostPage(DeclareLostViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Le bouton Retour système revient d'une étape (ou propose d'abandonner) au lieu de quitter brutalement.
    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }
}
