using LostFoundDocs.Api.Domain;
using LostFoundDocs.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace LostFoundDocs.Api.Services;

/// <summary>Stockage local des photos de documents : JPEG ou PNG vérifiés par leur signature (et non par l'extension), taille limitée.</summary>
public sealed class PhotoStorage(IOptions<StorageOptions> options)
{
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public async Task<(string FileName, string ContentType)> SaveAsync(Guid reportId, Stream content, CancellationToken ct)
    {
        var maxBytes = options.Value.MaxPhotoMegabytes * 1024L * 1024L;
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > maxBytes)
                throw ApiException.BadRequest("photo_too_large", $"La photo dépasse {options.Value.MaxPhotoMegabytes} Mo.");
        }
        var bytes = buffer.ToArray();
        string ext, contentType;
        if (StartsWith(bytes, PngMagic)) (ext, contentType) = (".png", "image/png");
        else if (StartsWith(bytes, JpegMagic)) (ext, contentType) = (".jpg", "image/jpeg");
        else throw ApiException.BadRequest("photo_format", "Format non pris en charge : JPG ou PNG uniquement.");

        Directory.CreateDirectory(options.Value.PhotosPath);
        var fileName = reportId.ToString("N") + ext;
        await File.WriteAllBytesAsync(Path.Combine(options.Value.PhotosPath, fileName), bytes, ct);
        return (fileName, contentType);
    }

    public Stream? Open(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            return null;
        var path = Path.Combine(options.Value.PhotosPath, fileName);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    private static bool StartsWith(byte[] data, byte[] magic) => data.Length >= magic.Length && data.AsSpan(0, magic.Length).SequenceEqual(magic);
}
