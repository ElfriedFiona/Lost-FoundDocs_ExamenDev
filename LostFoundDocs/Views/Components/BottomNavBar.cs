using LostFoundDocs.Services.Abstractions;
using LostFoundDocs.UI;
using Microsoft.Maui.Controls.Shapes;

namespace LostFoundDocs.Views.Components;

/// <summary>Barre de navigation à 4 onglets de la maquette : pastille bleue sous l'icône active, pastille rouge de messages.</summary>
public sealed class BottomNavBar : ContentView
{
    public static readonly BindableProperty SelectedTabProperty =
        BindableProperty.Create(nameof(SelectedTab), typeof(AppTab), typeof(BottomNavBar), AppTab.Home,
            propertyChanged: (b, _, _) => ((BottomNavBar)b).Build());
    public static readonly BindableProperty MessagesBadgeProperty =
        BindableProperty.Create(nameof(MessagesBadge), typeof(int), typeof(BottomNavBar), 0,
            propertyChanged: (b, _, _) => ((BottomNavBar)b).Build());

    public BottomNavBar() => Build();

    public AppTab SelectedTab { get => (AppTab)GetValue(SelectedTabProperty); set => SetValue(SelectedTabProperty, value); }
    public int MessagesBadge { get => (int)GetValue(MessagesBadgeProperty); set => SetValue(MessagesBadgeProperty, value); }

    private void Build()
    {
        var grid = new Grid
        {
            BackgroundColor = Res.GetColor("White"),
            Padding = new Thickness(8, 8, 8, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star),
            },
        };
        grid.Add(Item(AppTab.Home, Icons.House, "Accueil", "home"), 0);
        grid.Add(Item(AppTab.Reports, Icons.ClipboardList, "Signalements", "reports"), 1);
        grid.Add(Item(AppTab.Messages, Icons.MessageSquare, "Messages", "messages"), 2);
        grid.Add(Item(AppTab.Profile, Icons.User, "Profil", "profile"), 3);

        Content = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { new BoxView { HeightRequest = 1, Color = Res.GetColor("Border") }, grid },
        };
    }

    private View Item(AppTab tab, string glyph, string label, string route)
    {
        var selected = tab == SelectedTab;
        var accent = Res.GetColor(selected ? "Primary" : "TextSecondary");

        var pill = new Border
        {
            BackgroundColor = selected ? Res.GetColor("PrimarySoft") : Colors.Transparent,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(15) },
            WidthRequest = 56,
            HeightRequest = 30,
            Content = new Label
            {
                Text = glyph,
                FontFamily = Icons.FontFamily,
                FontSize = 22,
                TextColor = accent,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        };
        var iconHost = new Grid { WidthRequest = 56, HeightRequest = 30, HorizontalOptions = LayoutOptions.Center };
        iconHost.Add(pill);

        var unread = tab == AppTab.Messages ? MessagesBadge : 0;
        if (unread > 0)
        {
            iconHost.Add(new Border
            {
                BackgroundColor = Res.GetColor("Error"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(9) },
                WidthRequest = 18,
                HeightRequest = 18,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, -6, -4, 0),
                Content = new Label
                {
                    Text = unread > 9 ? "9+" : unread.ToString(),
                    FontSize = 11,
                    FontFamily = "OpenSansSemibold",
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            });
        }

        var stack = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                iconHost,
                new Label
                {
                    Text = label,
                    FontSize = 12,
                    FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular",
                    TextColor = accent,
                    HorizontalTextAlignment = TextAlignment.Center,
                },
            },
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (tab != SelectedTab)
            {
                await ServiceHelper.Get<INavigationService>().GoToAsync("//" + route);
            }
        };
        stack.GestureRecognizers.Add(tap);
        SemanticProperties.SetDescription(stack, unread > 0 ? $"{label}, {unread} non lu" : label);
        SemanticProperties.SetHint(stack, selected ? "Onglet actif" : "Ouvrir cet onglet");
        return stack;
    }
}
