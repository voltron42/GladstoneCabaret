using System.Text;

namespace FetchLrclib.IntegrationTests.EndToEnd;

/// <summary>Whole runs through the real components, with the network and file system replaced.</summary>
public class FetchRunTests
{
    private sealed class FakeClient : ILrclibClient
    {
        public Task<FetchResponse> GetRecordAsync(long recordId) => Task.FromResult(recordId == 1
            ? new FetchResponse(200, Encoding.UTF8.GetBytes(
                """{"id":1,"artistName":"A","trackName":"One","instrumental":false,"plainLyrics":"made up"}"""), false)
            : new FetchResponse(404, [], false));
    }

    private sealed class MemoryFileSystem : IFileSystem
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public bool FileExists(string path) => Files.ContainsKey(path);

        public void CreateDirectory(string path)
        {
        }

        public void WriteAllBytes(string path, byte[] content) => Files[path] = content;

        public void MoveWithoutReplacing(string source, string destination)
        {
            Files.Add(destination, Files[source]);
            Files.Remove(source);
        }

        public void DeleteFile(string path) => Files.Remove(path);
    }

    [Fact]
    public async Task A_second_run_keeps_files_including_corrections()
    {
        var fileSystem = new MemoryFileSystem();
        var dependencies = new Dependencies(
            _ => """[{"artist":"A","songTitle":"One","lrclibRecordId":1},{"artist":"B","songTitle":"Two","lrclibRecordId":2}]""",
            new FakeClient(),
            directory => new LyricFileStore(fileSystem, directory));
        string[] arguments = ["--songs", "list", "--output", "out"];
        var corrected = Path.Combine("out", "A - One.txt");

        using var firstOutput = new StringWriter();
        var firstExitCode = await App.RunAsync(arguments, dependencies, firstOutput, TextWriter.Null);
        fileSystem.Files[corrected] = "corrected"u8.ToArray();
        using var secondOutput = new StringWriter();
        await App.RunAsync(arguments, dependencies, secondOutput, TextWriter.Null);

        Assert.Equal(2, firstExitCode);
        Assert.Equal(
            "GO     A - One (line times: no; saved: 2, kept: 0)\nERROR  B - Two (record not found)\nSummary: go=1, no-go=0, error=1\n",
            firstOutput.ToString());
        Assert.StartsWith("GO     A - One (line times: no; saved: 0, kept: 2)\n", secondOutput.ToString());
        Assert.Equal("corrected"u8.ToArray(), fileSystem.Files[corrected]);
        Assert.Equal(2, fileSystem.Files.Count);
    }
}
