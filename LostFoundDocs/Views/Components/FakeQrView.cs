namespace LostFoundDocs.Views.Components;

/// <summary>Illustration non scannable du reçu de démonstration, sans données personnelles.</summary>
public sealed class FakeQrView : GraphicsView
{
    public FakeQrView()
    {
        Drawable = new DemoPattern();
        HeightRequest = WidthRequest = 120;
        HorizontalOptions = LayoutOptions.Center;
        SemanticProperties.SetDescription(this, "QR fictif, non scannable et non vérifiable");
    }
    private sealed class DemoPattern : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(bounds);
            canvas.FillColor = Colors.DarkSlateGray;
            const int count = 15;
            var size = Math.Min(bounds.Width, bounds.Height) / count;
            for (var y = 1; y < count - 1; y++)
                for (var x = 1; x < count - 1; x++)
                    if ((x * 3 + y * 7) % 5 < 2 && (y < 5 || y > 9))
                        canvas.FillRectangle(x * size, y * size, size, size);
            canvas.FontColor = Colors.DarkSlateGray;
            canvas.FontSize = 18;
            canvas.DrawString("DÉMO", 0, bounds.Height / 2 - 12, bounds.Width, 24, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
