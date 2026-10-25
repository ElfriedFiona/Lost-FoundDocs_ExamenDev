using LostFoundDocs.UI;

namespace LostFoundDocs.Views.Components;

/// <summary>Barre de progression à segments (4 étapes de la déclaration).</summary>
public sealed class StepProgressBar : ContentView
{
    public static readonly BindableProperty TotalProperty = Prop(nameof(Total), 4);
    public static readonly BindableProperty CurrentProperty = Prop(nameof(Current), 1);

    private static BindableProperty Prop(string name, int value) =>
        BindableProperty.Create(name, typeof(int), typeof(StepProgressBar), value,
            propertyChanged: (b, _, _) => ((StepProgressBar)b).Build());

    public StepProgressBar() => Build();

    public int Total { get => (int)GetValue(TotalProperty); set => SetValue(TotalProperty, value); }
    public int Current { get => (int)GetValue(CurrentProperty); set => SetValue(CurrentProperty, value); }

    private void Build()
    {
        var grid = new Grid { ColumnSpacing = 6 };
        for (var i = 0; i < Total; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.Add(new BoxView
            {
                HeightRequest = 6,
                CornerRadius = 3,
                Color = Res.GetColor(i < Current ? "Primary" : "Border"),
            }, i);
        }
        Content = grid;
        SemanticProperties.SetDescription(this, $"Étape {Current} sur {Total}");
    }
}
