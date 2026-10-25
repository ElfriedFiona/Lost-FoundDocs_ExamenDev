using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LostFoundDocs.Api.Infrastructure;

/// <summary>
/// Toutes les dates/heures de l'API sont en UTC et sérialisées avec le suffixe « Z ». SQLite renvoie des dates sans « Kind » :
/// sans ce convertisseur, un client interpréterait « 2026-10-22T11:21:17 » comme une heure locale.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
    }
}
