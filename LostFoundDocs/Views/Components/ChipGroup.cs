using System.Windows.Input;
using LostFoundDocs.UI;
using Microsoft.Maui.Controls.Shapes;

namespace LostFoundDocs.Views.Components;

/// <summary>Puces à passage à la ligne. Avec SelectOnTap, la puce touchée devient l'élément sélectionné (créneaux).</summary>
public sealed class ChipGroup : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IEnumerable<string>), typeof(ChipGroup), null,
            propertyChanged: (b, _, _) => ((ChipGroup)b).Build());
    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(string), typeof(ChipGroup), null, BindingMode.TwoWay,
            propertyChanged: (b, _, _) => ((ChipGroup)b).Build());
    public static readonly BindableProperty SelectOnTapProperty =
        BindableProperty.Create(nameof(SelectOnTap), typeof(bool), typeof(ChipGroup), false);
    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(ChipGroup), null);

    public IEnumerable<string>? Items { get => (IEnumerable<string>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public string? SelectedItem { get => (string?)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public bool SelectOnTap { get => (bool)GetValue(SelectOnTapProperty); set => SetValue(SelectOnTapProperty, value); }
    public ICommand? ItemTappedCommand { get => (ICommand?)GetValue(ItemTappedCommandProperty); set => SetValue(ItemTappedCommandProperty, value); }

    private void Build()
    {
        var flex = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Direction = Microsoft.Maui.Layouts.FlexDirection.Row };
        foreach (var item in Items ?? Enumerable.Empty<string>())
        {
            var selected = item == SelectedItem;
            var chip = new Border
            {
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(14, 8),
                BackgroundColor = Res.GetColor(selected ? "PrimarySoft" : "White"),
                Stroke = Res.GetBrush(selected ? "PrimaryBrush" : "BorderBrush"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
                Content = new Label
                {
                    Text = item,
                    FontSize = 14,
                    FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular",
                    TextColor = Res.GetColor(selected ? "Primary" : "TextPrimary"),
                },
            };
            var captured = item;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (SelectOnTap) SelectedItem = captured;
                if (ItemTappedCommand?.CanExecute(captured) == true) ItemTappedCommand.Execute(captured);
            };
            chip.GestureRecognizers.Add(tap);
            SemanticProperties.SetDescription(chip, selected ? $"{item}, sélectionné" : item);
            flex.Add(chip);
        }
        Content = flex;
    }
}
