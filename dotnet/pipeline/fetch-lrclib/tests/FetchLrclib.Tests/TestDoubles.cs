using System.Text;

namespace FetchLrclib.Tests;

internal static class Records
{
    public static byte[] Bytes(string json) => Encoding.UTF8.GetBytes(json);

    public static FetchResponse Usable(
        long id = 1, string plain = "\"made up line\"", string synced = "null", string instrumental = "false") =>
        new(
            200,
            Bytes($$"""{"id":{{id}},"artistName":"Artist","trackName":"Track","instrumental":{{instrumental}},"plainLyrics":{{plain}},"syncedLyrics":{{synced}}}"""),
            false);
}

internal sealed class FakeClient(params FetchResponse[] responses) : ILrclibClient
{
    private readonly Queue<FetchResponse> remaining = new(responses);

    public List<long> RequestedIds { get; } = [];

    public Task<FetchResponse> GetRecordAsync(long recordId)
    {
        RequestedIds.Add(recordId);
        return Task.FromResult(remaining.Dequeue());
    }
}

internal sealed class FakeStore : ILyricFileStore
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public Exception? Failure { get; init; }

    public bool SaveIfAbsent(string fileName, byte[] content) =>
        Failure is null ? Files.TryAdd(fileName, content) : throw Failure;
}

internal sealed class FakeFileSystem : IFileSystem
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public bool FailWrite { get; init; }

    /// <summary>Simulates another process creating the destination just before the move.</summary>
    public bool LoseRace { get; init; }

    public bool FileExists(string path) => Files.ContainsKey(path);

    public void CreateDirectory(string path)
    {
    }

    public void WriteAllBytes(string path, byte[] content)
    {
        Files[path] = content;
        if (FailWrite)
        {
            throw new IOException("disk full");
        }
    }

    public void MoveWithoutReplacing(string source, string destination)
    {
        if (LoseRace)
        {
            Files[destination] = [9];
        }

        if (Files.ContainsKey(destination))
        {
            throw new IOException("destination exists");
        }

        Files[destination] = Files[source];
        Files.Remove(source);
    }

    public void DeleteFile(string path) => Files.Remove(path);
}
