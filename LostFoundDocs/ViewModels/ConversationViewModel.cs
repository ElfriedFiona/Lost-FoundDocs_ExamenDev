using System.Windows.Input;
using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.Services.Navigation;
namespace LostFoundDocs.ViewModels;

public sealed class ConversationViewModel : ReportContextViewModel
{
    private readonly IConversationService _conversations;
    private string _message = "";
    public Conversation? Conversation { get; private set; }
    public ConversationViewModel(IReportService reports, INavigationService nav, IConversationService conversations) : base(reports, nav)
    {
        _conversations = conversations;
        SendCommand = new AsyncCommand(async () =>
        {
            await conversations.SendAsync(ReportId, Message);
            Message = "";
        }, () => !string.IsNullOrWhiteSpace(Message) && CanWrite);
        RestitutionCommand = new AsyncCommand(() => OpenAsync(Routes.Restitution));
        ReceiptCommand = new AsyncCommand(() => OpenAsync(Routes.Receipt));
    }
    public string Message { get => _message; set { SetProperty(ref _message, value); SendCommand.RaiseCanExecuteChanged(); } }
    public AsyncCommand SendCommand { get; }
    public ICommand RestitutionCommand { get; }
    public ICommand ReceiptCommand { get; }
    public bool CanWrite => Report?.Status is ReportStatus.MatchFound or ReportStatus.MatchConfirmed;
    public bool IsClosed => !CanWrite;
    public bool IsReturned => Report?.Status == ReportStatus.Returned;
    protected override async Task LoadAsync()
    {
        Conversation = await _conversations.OpenAsync(ReportId);
        SendCommand.RaiseCanExecuteChanged();
    }
}
