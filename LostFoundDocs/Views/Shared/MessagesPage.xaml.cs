using LostFoundDocs.ViewModels;
namespace LostFoundDocs.Views.Shared;
public partial class MessagesPage : ContentPage
{
    private readonly MessagesViewModel _viewModel;
    public MessagesPage(MessagesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing() { base.OnAppearing(); _viewModel.Refresh(); }
}
