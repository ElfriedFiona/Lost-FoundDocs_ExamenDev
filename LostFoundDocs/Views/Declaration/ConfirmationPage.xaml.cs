using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Declaration;
public partial class ConfirmationPage : ContentPage
{
    private readonly ReportDetailsViewModel _viewModel;
    public ConfirmationPage(ReportDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.RefreshCommand.Execute(null); }
}
