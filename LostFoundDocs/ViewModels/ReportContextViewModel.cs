using LostFoundDocs.Models;
using LostFoundDocs.Services.Abstractions;
namespace LostFoundDocs.ViewModels;

public abstract class ReportContextViewModel : ViewModelBase, IQueryAttributable
{
    protected readonly IReportService Reports;
    protected readonly INavigationService Nav;
    protected Guid ReportId;
    public Report? Report { get; private set; }
    public AsyncCommand RefreshCommand { get; }
    protected ReportContextViewModel(IReportService reports, INavigationService nav)
    {
        Reports = reports; Nav = nav;
        RefreshCommand = new AsyncCommand(async () =>
        {
            IsBusy = true;
            try
            {
                Report = await Reports.GetAsync(ReportId) ?? throw new InvalidOperationException("Ce signalement n’est pas accessible dans cette session.");
                await LoadAsync();
                OnPropertyChanged(string.Empty);
            }
            finally { IsBusy = false; }
        });
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("reportId", out var value) && value is Guid id) ReportId = id;
    }
    protected virtual Task LoadAsync() => Task.CompletedTask;
    protected Task OpenAsync(string route) => Nav.GoToAsync(route, new Dictionary<string, object> { ["reportId"] = ReportId });
}
