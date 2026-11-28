using System.Text.Json;

namespace FetchLrclib;

public sealed record LyricRecord(
    string ArtistName,
    string TrackName,
    bool Instrumental,
    string? PlainLyrics,
    string? SyncedLyrics)
{
    /// <summary>Returns null when the body is not a usable record for the requested ID.</summary>
    public static LyricRecord? TryParse(ReadOnlyMemory<byte> body, long expectedId)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("id", out var id)
                || id.ValueKind != JsonValueKind.Number
                || !id.TryGetInt64(out var actualId)
                || actualId != expectedId
                || !root.TryGetProperty("instrumental", out var instrumental)
                || instrumental.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                || !TryGetOptionalText(root, "artistName", out var artistName)
                || string.IsNullOrWhiteSpace(artistName)
                || !TryGetOptionalText(root, "trackName", out var trackName)
                || string.IsNullOrWhiteSpace(trackName)
                || !TryGetOptionalText(root, "plainLyrics", out var plainLyrics)
                || !TryGetOptionalText(root, "syncedLyrics", out var syncedLyrics))
            {
                return null;
            }

            return new LyricRecord(artistName, trackName, instrumental.GetBoolean(), plainLyrics, syncedLyrics);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TryGetOptionalText(JsonElement root, string name, out string? text)
    {
        text = null;
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        text = value.GetString();
        return true;
    }
}
