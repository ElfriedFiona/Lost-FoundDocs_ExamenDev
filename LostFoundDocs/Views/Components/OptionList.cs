using LostFoundDocs.UI;
using Microsoft.Maui.Controls.Shapes;

namespace LostFoundDocs.Views.Components;

/// <summary>Liste à choix unique (boutons radio) : motifs d'erreur, lieux de restitution.</summary>
public sealed class OptionList : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IEnumerable<OptionItem>), typeof(OptionList), null,
            propertyChanged: (b, _, _) => ((OptionList)b).Build());
    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(OptionItem), typeof(OptionList), null, BindingMode.TwoWay,
            propertyChanged: (b, _, _) => ((OptionList)b).Build());

    public IEnumerable<OptionItem>? Items { get => (IEnumerable<OptionItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public OptionItem? SelectedItem { get => (OptionItem?)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    private void Build()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        foreach (var item in Items ?? Enumerable.Empty<OptionItem>())
        {
            var selected = SelectedItem is not null && SelectedItem.Id == item.Id;

            var radio = new Border
            {
                WidthRequest = 22, HeightRequest = 22, StrokeThickness = 2,
                Stroke = Res.GetBrush(selected ? "PrimaryBrush" : "BorderBrush"),
                BackgroundColor = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(11) },
                VerticalOptions = LayoutOptions.Center,
                Content = selected
                    ? new BoxView { WidthRequest = 10, HeightRequest = 10, CornerRadius = 5, Color = Res.GetColor("Primary"),
                                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                    : null,
            };

            var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            text.Add(new Label { Text = item.Title, FontSize = 15, FontFamily = "OpenSansSemibold", TextColor = Res.GetColor("TextPrimary") });
            if (!string.IsNullOrEmpty(item.Subtitle))
                text.Add(new Label { Text = item.Subtitle, FontSize = 13, TextColor = Res.GetColor("TextSecondary") });

            var row = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto) } };
            row.Add(radio, 0);
            var next = 1;
            if (!string.IsNullOrEmpty(item.Glyph))
            {
                row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                row.Add(new Label { Text = item.Glyph, FontFamily = Icons.FontFamily, FontSize = 20, TextColor = Res.GetColor("Primary"),
                                    VerticalOptions = LayoutOptions.Center }, next++);
            }
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            row.Add(text, next);

            var card = new Border
            {
                Padding = new Thickness(14, 12),
                BackgroundColor = Res.GetColor(selected ? "PrimarySoft" : "White"),
                Stroke = Res.GetBrush(selected ? "PrimaryBrush" : "BorderBrush"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
                Content = row,
            };
            var captured = item;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => SelectedItem = captured;
            card.GestureRecognizers.Add(tap);
            SemanticProperties.SetDescription(card, string.IsNullOrEmpty(item.Subtitle) ? item.Title : $"{item.Title}, {item.Subtitle}");
            SemanticProperties.SetHint(card, selected ? "Sélectionné" : "Appuyer pour sélectionner");
            stack.Add(card);
        }
        Content = stack;
    }
}
