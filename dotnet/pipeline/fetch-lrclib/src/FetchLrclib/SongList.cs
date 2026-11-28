using System.Text.Json;

namespace FetchLrclib;

public sealed record Song(string Artist, string SongTitle, long LrclibRecordId);

public sealed class SongListException(string message) : Exception(message);

public static class SongList
{
    public static IReadOnlyList<Song> Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                throw new SongListException("song list must be a non-empty JSON array");
            }

            return root.EnumerateArray().Select(ParseSong).ToList();
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new SongListException("song list is not valid JSON text");
        }
    }

    private static Song ParseSong(JsonElement entry, int index)
    {
        var position = index + 1;
        if (entry.ValueKind != JsonValueKind.Object)
        {
            throw new SongListException($"song {position} must be an object");
        }

        return new Song(
            RequireText(entry, "artist", position),
            RequireText(entry, "songTitle", position),
            RequireRecordId(entry, position));
    }

    private static string RequireText(JsonElement entry, string name, int position)
    {
        if (entry.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()))
        {
            return value.GetString()!;
        }

        throw new SongListException($"song {position}: {name} must be a non-blank string");
    }

    private static long RequireRecordId(JsonElement entry, int position)
    {
        if (entry.TryGetProperty("lrclibRecordId", out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var recordId)
            && recordId > 0)
        {
            return recordId;
        }

        throw new SongListException($"song {position}: lrclibRecordId must be a positive integer");
    }
}
