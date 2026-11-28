namespace FetchLrclib.IntegrationTests.External;

public sealed class PhysicalFileSystemTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("fetch-lrclib-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Store_creates_the_directory_saves_then_keeps_without_leftovers()
    {
        var output = Path.Combine(root, "nested", "out");
        var store = new LyricFileStore(new PhysicalFileSystem(), output);

        Assert.True(store.SaveIfAbsent("song.txt", [1, 2]));
        Assert.False(store.SaveIfAbsent("song.txt", [3]));

        Assert.Equal([Path.Combine(output, "song.txt")], Directory.GetFiles(output));
        Assert.Equal([1, 2], File.ReadAllBytes(Path.Combine(output, "song.txt")));
    }

    [Fact]
    public void MoveWithoutReplacing_refuses_to_overwrite_like_the_stand_in()
    {
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        File.WriteAllBytes(source, [1]);
        File.WriteAllBytes(destination, [2]);

        Assert.Throws<IOException>(() => new PhysicalFileSystem().MoveWithoutReplacing(source, destination));
        Assert.Equal([2], File.ReadAllBytes(destination));
    }
}
