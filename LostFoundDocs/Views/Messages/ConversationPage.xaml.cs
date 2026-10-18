using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Messages;
public partial class ConversationPage : ContentPage
{
    private readonly ConversationViewModel _viewModel;
    public ConversationPage(ConversationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.RefreshCommand.Execute(null); }
}
