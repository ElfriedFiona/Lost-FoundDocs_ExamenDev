namespace LostFoundDocs.Views.Components;

/// <summary>
/// SIMULATION (frontend uniquement) : motif de type QR code dessiné localement à partir d'un texte.
/// Il n'est PAS scannable et ne prouve rien ; le vrai code de vérification sera fourni par le backend.
/// </summary>
public sealed class FakeQrView : GraphicsView, IDrawable
{
    private const int Modules = 21;

    public static readonly BindableProperty PayloadProperty =
        BindableProperty.Create(nameof(Payload), typeof(string), typeof(FakeQrView), string.Empty,
            propertyChanged: (b, _, _) => ((FakeQrView)b).Invalidate());

    public FakeQrView()
    {
        Drawable = this;
        WidthRequest = 96;
        HeightRequest = 96;
        SemanticProperties.SetDescription(this, "Code QR de démonstration, non scannable");
    }

    public string Payload { get => (string)GetValue(PayloadProperty); set => SetValue(PayloadProperty, value); }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cell = Math.Min(dirtyRect.Width, dirtyRect.Height) / Modules;
        var seed = (uint)Payload.Aggregate(2166136261u, (h, c) => unchecked((h ^ c) * 16777619u));

        bool IsFinder(int x, int y, out bool dark)
        {
            dark = false;
            foreach (var (ox, oy) in new[] { (0, 0), (Modules - 7, 0), (0, Modules - 7) })
            {
                if (x < ox || x >= ox + 7 || y < oy || y >= oy + 7) continue;
                var dx = x - ox; var dy = y - oy;
                var ring = dx == 0 || dx == 6 || dy == 0 || dy == 6;
                var center = dx is >= 2 and <= 4 && dy is >= 2 and <= 4;
                dark = ring || center;
                return true;
            }
            return false;
        }

        canvas.FillColor = Color.FromArgb("#17202A");
        var state = seed == 0 ? 1u : seed;
        for (var y = 0; y < Modules; y++)
        {
            for (var x = 0; x < Modules; x++)
            {
                state = unchecked(state * 1664525u + 1013904223u);
                var fill = IsFinder(x, y, out var dark) ? dark : (state >> 16 & 1) == 1;
                if (fill) canvas.FillRectangle(x * cell, y * cell, cell, cell);
            }
        }
    }
}
