using System.Text;

namespace LostFoundDocs.Api.Qr;

/// <summary>
/// Encodeur de QR code (mode octets, correction d'erreurs M, versions 1 à 10, jusqu'à 213 octets),
/// sans dépendance externe. Renvoie la matrice de modules (true = sombre), sans zone de silence.
/// </summary>
public static class QrEncoder
{
    private static readonly int[] EccPerBlockM = { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26 };
    private static readonly int[] BlocksM = { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5 };

    public static bool[][] Encode(string text)
    {
        var data = Encoding.UTF8.GetBytes(text);
        int version = 0;
        for (var v = 1; v <= 10; v++)
        {
            var capBits = DataCodewords(v) * 8;
            var ccBits = v <= 9 ? 8 : 16;
            if (4 + ccBits + data.Length * 8 <= capBits) { version = v; break; }
        }
        if (version == 0) throw new ArgumentException("Texte trop long pour le QR code (213 octets maximum).");

        var codewords = AddEcc(BuildDataCodewords(data, version), version);
        var size = 17 + 4 * version;
        var modules = new bool[size][];
        var isFunction = new bool[size][];
        for (var i = 0; i < size; i++) { modules[i] = new bool[size]; isFunction[i] = new bool[size]; }

        DrawFunctionPatterns(modules, isFunction, version);
        DrawCodewords(modules, isFunction, codewords);

        var bestMask = 0;
        var bestPenalty = int.MaxValue;
        for (var mask = 0; mask < 8; mask++)
        {
            ApplyMask(modules, isFunction, mask);
            DrawFormatBits(modules, isFunction, mask);
            var penalty = Penalty(modules);
            if (penalty < bestPenalty) { bestPenalty = penalty; bestMask = mask; }
            ApplyMask(modules, isFunction, mask); // annule (XOR)
        }
        ApplyMask(modules, isFunction, bestMask);
        DrawFormatBits(modules, isFunction, bestMask);
        LastMask = bestMask;
        LastVersion = version;
        return modules;
    }

    /// <summary>Informations de la dernière génération (utilisées par les tests).</summary>
    public static int LastMask { get; private set; }
    public static int LastVersion { get; private set; }

    public static string[] ToRows(bool[][] modules) =>
        modules.Select(r => new string(r.Select(b => b ? '1' : '0').ToArray())).ToArray();

    // ---------- capacité ----------
    private static int RawCodewords(int version)
    {
        var result = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            var numAlign = version / 7 + 2;
            result -= (25 * numAlign - 10) * numAlign - 55;
            if (version >= 7) result -= 36;
        }
        return result / 8;
    }

    private static int DataCodewords(int version) => RawCodewords(version) - EccPerBlockM[version] * BlocksM[version];

    // ---------- données ----------
    private static byte[] BuildDataCodewords(byte[] data, int version)
    {
        var bits = new List<bool>();
        void Append(int value, int count) { for (var i = count - 1; i >= 0; i--) bits.Add(((value >> i) & 1) != 0); }

        Append(0b0100, 4);
        Append(data.Length, version <= 9 ? 8 : 16);
        foreach (var b in data) Append(b, 8);

        var capacity = DataCodewords(version) * 8;
        Append(0, Math.Min(4, capacity - bits.Count));
        Append(0, (8 - bits.Count % 8) % 8);
        for (var pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11) Append(pad, 8);

        var result = new byte[bits.Count / 8];
        for (var i = 0; i < bits.Count; i++)
            if (bits[i]) result[i >> 3] |= (byte)(1 << (7 - (i & 7)));
        return result;
    }

    private static byte[] AddEcc(byte[] data, int version)
    {
        var numBlocks = BlocksM[version];
        var eccLen = EccPerBlockM[version];
        var raw = RawCodewords(version);
        var numShort = numBlocks - raw % numBlocks;
        var shortLen = raw / numBlocks;

        var blocks = new List<byte[]>();
        var rsDiv = ReedSolomonDivisor(eccLen);
        for (int i = 0, k = 0; i < numBlocks; i++)
        {
            var datLen = shortLen - eccLen + (i < numShort ? 0 : 1);
            var dat = new byte[datLen];
            Array.Copy(data, k, dat, 0, datLen);
            k += datLen;
            var ecc = ReedSolomonRemainder(dat, rsDiv);
            var block = new byte[shortLen + 1];
            Array.Copy(dat, block, datLen);
            Array.Copy(ecc, 0, block, datLen + (i < numShort ? 1 : 0), eccLen);
            blocks.Add(i < numShort ? block : block);
            if (i < numShort)
            {
                // bloc court : l'emplacement vide (index datLen) est ignoré à l'entrelacement
                blocks[i] = block;
            }
        }

        var result = new List<byte>();
        for (var i = 0; i < shortLen + 1; i++)
        {
            for (var j = 0; j < blocks.Count; j++)
            {
                if (i != shortLen - eccLen || j >= numShort) result.Add(blocks[j][i]);
            }
        }
        return result.ToArray();
    }

    private static byte[] ReedSolomonDivisor(int degree)
    {
        var result = new byte[degree];
        result[degree - 1] = 1;
        var root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                result[j] = (byte)Multiply(result[j], root);
                if (j + 1 < degree) result[j] ^= result[j + 1];
            }
            root = Multiply(root, 0x02);
        }
        return result;
    }

    private static byte[] ReedSolomonRemainder(byte[] data, byte[] divisor)
    {
        var result = new byte[divisor.Length];
        foreach (var b in data)
        {
            var factor = b ^ result[0];
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[^1] = 0;
            for (var i = 0; i < result.Length; i++) result[i] ^= (byte)Multiply(divisor[i], factor);
        }
        return result;
    }

    private static int Multiply(int x, int y)
    {
        var z = 0;
        for (var i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return z;
    }

    // ---------- dessin ----------
    private static void SetFunction(bool[][] modules, bool[][] isFunction, int x, int y, bool dark)
    {
        modules[y][x] = dark;
        isFunction[y][x] = true;
    }

    private static void DrawFunctionPatterns(bool[][] m, bool[][] f, int version)
    {
        var size = m.Length;
        for (var i = 0; i < size; i++)
        {
            SetFunction(m, f, 6, i, i % 2 == 0);
            SetFunction(m, f, i, 6, i % 2 == 0);
        }
        DrawFinder(m, f, 3, 3);
        DrawFinder(m, f, size - 4, 3);
        DrawFinder(m, f, 3, size - 4);

        var align = AlignmentPositions(version, size);
        var n = align.Length;
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == n - 1) || (i == n - 1 && j == 0)) continue;
                DrawAlignment(m, f, align[i], align[j]);
            }
        }

        DrawFormatBits(m, f, 0);
        DrawVersion(m, f, version);
    }

    private static int[] AlignmentPositions(int version, int size)
    {
        if (version == 1) return Array.Empty<int>();
        var numAlign = version / 7 + 2;
        var step = (version * 4 + numAlign * 2 + 1) / (numAlign * 2 - 2) * 2;
        var result = new int[numAlign];
        result[0] = 6;
        for (int i = numAlign - 1, pos = size - 7; i >= 1; i--, pos -= step) result[i] = pos;
        return result;
    }

    private static void DrawFinder(bool[][] m, bool[][] f, int cx, int cy)
    {
        var size = m.Length;
        for (var dy = -4; dy <= 4; dy++)
        {
            for (var dx = -4; dx <= 4; dx++)
            {
                var dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                int x = cx + dx, y = cy + dy;
                if (x >= 0 && x < size && y >= 0 && y < size) SetFunction(m, f, x, y, dist != 2 && dist != 4);
            }
        }
    }

    private static void DrawAlignment(bool[][] m, bool[][] f, int cx, int cy)
    {
        for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
                SetFunction(m, f, cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
    }

    private static void DrawFormatBits(bool[][] m, bool[][] f, int mask)
    {
        var size = m.Length;
        const int eccFormatBitsM = 0; // niveau M
        var data = eccFormatBitsM << 3 | mask;
        var rem = data;
        for (var i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        var bits = (data << 10 | rem) ^ 0x5412;

        for (var i = 0; i <= 5; i++) SetFunction(m, f, 8, i, Bit(bits, i));
        SetFunction(m, f, 8, 7, Bit(bits, 6));
        SetFunction(m, f, 8, 8, Bit(bits, 7));
        SetFunction(m, f, 7, 8, Bit(bits, 8));
        for (var i = 9; i < 15; i++) SetFunction(m, f, 14 - i, 8, Bit(bits, i));

        for (var i = 0; i < 8; i++) SetFunction(m, f, size - 1 - i, 8, Bit(bits, i));
        for (var i = 8; i < 15; i++) SetFunction(m, f, 8, size - 15 + i, Bit(bits, i));
        SetFunction(m, f, 8, size - 8, true);
    }

    private static void DrawVersion(bool[][] m, bool[][] f, int version)
    {
        if (version < 7) return;
        var size = m.Length;
        var rem = version;
        for (var i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        var bits = version << 12 | rem;
        for (var i = 0; i < 18; i++)
        {
            var bit = Bit(bits, i);
            var a = size - 11 + i % 3;
            var b = i / 3;
            SetFunction(m, f, a, b, bit);
            SetFunction(m, f, b, a, bit);
        }
    }

    private static bool Bit(int value, int i) => ((value >> i) & 1) != 0;

    private static void DrawCodewords(bool[][] m, bool[][] f, byte[] data)
    {
        var size = m.Length;
        var i = 0;
        for (var right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (var vert = 0; vert < size; vert++)
            {
                for (var j = 0; j < 2; j++)
                {
                    var x = right - j;
                    var upward = ((right + 1) & 2) == 0;
                    var y = upward ? size - 1 - vert : vert;
                    if (!f[y][x] && i < data.Length * 8)
                    {
                        m[y][x] = Bit(data[i >> 3], 7 - (i & 7));
                        i++;
                    }
                }
            }
        }
    }

    private static void ApplyMask(bool[][] m, bool[][] f, int mask)
    {
        var size = m.Length;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (f[y][x]) continue;
                var invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => (x / 3 + y / 2) % 2 == 0,
                    5 => x * y % 2 + x * y % 3 == 0,
                    6 => (x * y % 2 + x * y % 3) % 2 == 0,
                    _ => ((x + y) % 2 + x * y % 3) % 2 == 0,
                };
                if (invert) m[y][x] = !m[y][x];
            }
        }
    }

    // ---------- pénalité (choix du masque) ----------
    private static int Penalty(bool[][] m)
    {
        var size = m.Length;
        var result = 0;

        // Règle 1 : séries de 5 modules identiques ou plus
        for (var pass = 0; pass < 2; pass++)
        {
            for (var a = 0; a < size; a++)
            {
                var run = 1;
                for (var b = 1; b < size; b++)
                {
                    var cur = pass == 0 ? m[a][b] : m[b][a];
                    var prev = pass == 0 ? m[a][b - 1] : m[b - 1][a];
                    if (cur == prev) run++;
                    else { if (run >= 5) result += 3 + (run - 5); run = 1; }
                }
                if (run >= 5) result += 3 + (run - 5);
            }
        }

        // Règle 2 : blocs 2x2
        for (var y = 0; y < size - 1; y++)
            for (var x = 0; x < size - 1; x++)
                if (m[y][x] == m[y][x + 1] && m[y][x] == m[y + 1][x] && m[y][x] == m[y + 1][x + 1]) result += 3;

        // Règle 3 : motifs ressemblant aux repères
        bool[] p1 = { true, false, true, true, true, false, true, false, false, false, false };
        bool[] p2 = { false, false, false, false, true, false, true, true, true, false, true };
        for (var pass = 0; pass < 2; pass++)
        {
            for (var a = 0; a < size; a++)
            {
                for (var b = 0; b <= size - 11; b++)
                {
                    bool match1 = true, match2 = true;
                    for (var k = 0; k < 11 && (match1 || match2); k++)
                    {
                        var v = pass == 0 ? m[a][b + k] : m[b + k][a];
                        if (v != p1[k]) match1 = false;
                        if (v != p2[k]) match2 = false;
                    }
                    if (match1) result += 40;
                    if (match2) result += 40;
                }
            }
        }

        // Règle 4 : équilibre sombre / clair
        var dark = m.Sum(r => r.Count(c => c));
        var total = size * size;
        var k4 = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        result += Math.Max(0, k4) * 10;
        return result;
    }
}
