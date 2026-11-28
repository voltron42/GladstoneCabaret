using System.Text;

namespace FetchLrclib;

public enum Outcome
{
    Go,
    NoGo,
    Error,
}

public sealed record SongResult(
    Song Song,
    Outcome Outcome,
    string Reason = "",
    bool HasLineTimes = false,
    int SavedCount = 0,
    int KeptCount = 0);

public sealed class FetchService(ILrclibClient client, ILyricFileStore store)
{
    private static readonly UTF8Encoding Utf8WithoutMark = new(encoderShouldEmitUTF8Identifier: false);

    public async Task<IReadOnlyList<SongResult>> FetchAllAsync(IReadOnlyList<Song> songs)
    {
        var results = new List<SongResult>();
        var stopped = false;
        foreach (var song in songs)
        {
            if (stopped)
            {
                results.Add(Error(song, "not attempted: service asked us to wait"));
                continue;
            }

            var response = await client.GetRecordAsync(song.LrclibRecordId);
            stopped = AsksUsToWait(response);
            results.Add(Evaluate(song, response));
        }

        return results;
    }

    private static bool AsksUsToWait(FetchResponse response) =>
        response.Status == 429 || (response.Status is not (null or 200) && response.HasRetryAfter);

    private static SongResult Error(Song song, string reason) => new(song, Outcome.Error, reason);

    private SongResult Evaluate(Song song, FetchResponse response)
    {
        if (response.Status != 200)
        {
            return Error(song, response.Status switch
            {
                null => "network failure",
                404 => "record not found",
                429 => "rate limited",
                _ => $"service error: HTTP {response.Status}",
            });
        }

        var record = LyricRecord.TryParse(response.Body, song.LrclibRecordId);
        if (record is null)
        {
            return Error(song, "invalid response");
        }

        if (record.Instrumental)
        {
            return new SongResult(song, Outcome.NoGo, "instrumental");
        }

        return string.IsNullOrWhiteSpace(record.PlainLyrics)
            ? new SongResult(song, Outcome.NoGo, "no plain lyrics")
            : Save(song, record, record.PlainLyrics, response.Body);
    }

    private SongResult Save(Song song, LyricRecord record, string plainLyrics, byte[] body)
    {
        var baseName = FileNames.BaseName(record.ArtistName, record.TrackName);
        var hasLineTimes = !string.IsNullOrWhiteSpace(record.SyncedLyrics);
        var files = new List<(string Name, byte[] Content)>
        {
            ($"{baseName}.json", body),
            ($"{baseName}.txt", Utf8WithoutMark.GetBytes(plainLyrics)),
        };
        if (hasLineTimes)
        {
            files.Add(($"{baseName}.lrc", Utf8WithoutMark.GetBytes(record.SyncedLyrics!)));
        }

        try
        {
            var savedCount = files.Count(file => store.SaveIfAbsent(file.Name, file.Content));
            return new SongResult(song, Outcome.Go, "", hasLineTimes, savedCount, files.Count - savedCount);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Error(song, "file write failed");
        }
    }
}
