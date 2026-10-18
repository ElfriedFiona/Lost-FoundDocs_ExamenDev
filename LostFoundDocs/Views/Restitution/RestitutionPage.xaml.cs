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
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.RefreshCommand.Execute(null); }
}
