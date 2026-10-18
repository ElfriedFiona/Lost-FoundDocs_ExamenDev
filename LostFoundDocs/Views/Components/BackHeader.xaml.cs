using System.Windows.Input;
using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>En-tête avec flèche de retour. Sans BackCommand, revient à l'écran précédent.</summary>
public partial class BackHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(BackHeader), string.Empty);
    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(BackHeader), null);

    public BackHeader() => InitializeComponent();

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public ICommand? BackCommand { get => (ICommand?)GetValue(BackCommandProperty); set => SetValue(BackCommandProperty, value); }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        if (BackCommand is { } cmd && cmd.CanExecute(null))
        {
            cmd.Execute(null);
            return;
        }
        await ServiceHelper.Get<INavigationService>().GoBackAsync();
    }
}
