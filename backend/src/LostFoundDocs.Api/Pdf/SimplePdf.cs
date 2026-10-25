using System.Globalization;
using System.Text;

namespace LostFoundDocs.Api.Pdf;

/// <summary>Générateur PDF minimal (une page A4, polices Helvetica intégrées, texte et rectangles), sans dépendance externe.</summary>
public sealed class SimplePdf
{
    private readonly StringBuilder _content = new();

    public void Text(double x, double y, double size, bool bold, string text)
    {
        _content.Append("BT /").Append(bold ? "F2" : "F1").Append(' ').Append(F(size)).Append(" Tf ")
            .Append(F(x)).Append(' ').Append(F(y)).Append(" Td (").Append(Escape(text)).Append(") Tj ET\n");
    }

    public void Rectangle(double x, double y, double w, double h, double gray = 0, bool fill = true)
    {
        _content.Append(F(gray)).Append(' ').Append(F(gray)).Append(' ').Append(F(gray)).Append(fill ? " rg " : " RG ")
            .Append(F(x)).Append(' ').Append(F(y)).Append(' ').Append(F(w)).Append(' ').Append(F(h)).Append(fill ? " re f\n" : " re S\n");
    }

    public void Line(double x1, double y1, double x2, double y2, double gray = 0.8)
    {
        _content.Append(F(gray)).Append(' ').Append(F(gray)).Append(' ').Append(F(gray)).Append(" RG 0.8 w ")
            .Append(F(x1)).Append(' ').Append(F(y1)).Append(" m ").Append(F(x2)).Append(' ').Append(F(y2)).Append(" l S\n");
    }

    public byte[] Build()
    {
        var latin1 = Encoding.Latin1;
        var content = latin1.GetBytes(_content.ToString());
        using var ms = new MemoryStream();
        var offsets = new List<long>();

        void Write(string s) { var b = latin1.GetBytes(s); ms.Write(b, 0, b.Length); }
        void BeginObject(int n) { offsets.Add(ms.Position); Write($"{n} 0 obj\n"); }

        Write("%PDF-1.4\n");
        BeginObject(1); Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        BeginObject(2); Write("<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        BeginObject(3); Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>\nendobj\n");
        BeginObject(4); Write($"<< /Length {content.Length} >>\nstream\n"); ms.Write(content, 0, content.Length); Write("\nendstream\nendobj\n");
        BeginObject(5); Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        BeginObject(6); Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        var xref = ms.Position;
        Write($"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) Write($"{o:D10} 00000 n \n");
        Write($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return ms.ToArray();
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            var ch = c switch { '’' => '\'', '‘' => '\'', '«' => '"', '»' => '"', '–' => '-', '—' => '-', '·' => '-', '…' => '.', _ => c };
            if (ch == '(' || ch == ')' || ch == '\\') sb.Append('\\').Append(ch);
            else if (ch > 0xFF) sb.Append('?');
            else sb.Append(ch);
        }
        return sb.ToString();
    }
}
