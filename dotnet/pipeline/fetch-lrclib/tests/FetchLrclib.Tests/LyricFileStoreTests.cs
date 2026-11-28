namespace FetchLrclib.Tests;

public class LyricFileStoreTests
{
    private static readonly string Destination = Path.Combine("out", "song.txt");

    [Fact]
    public void SaveIfAbsent_saves_and_leaves_no_temporary_file()
    {
        var fileSystem = new FakeFileSystem();

        var saved = new LyricFileStore(fileSystem, "out").SaveIfAbsent("song.txt", [1]);

        Assert.True(saved);
        Assert.Equal([Destination], fileSystem.Files.Keys);
        Assert.Equal([1], fileSystem.Files[Destination]);
    }

    [Fact]
    public void SaveIfAbsent_keeps_an_existing_file()
    {
        var fileSystem = new FakeFileSystem { Files = { [Destination] = [7] } };

        var saved = new LyricFileStore(fileSystem, "out").SaveIfAbsent("song.txt", [1]);

        Assert.False(saved);
        Assert.Equal([7], fileSystem.Files[Destination]);
    }

    [Fact]
    public void SaveIfAbsent_keeps_a_file_created_during_the_save()
    {
        var fileSystem = new FakeFileSystem { LoseRace = true };

        var saved = new LyricFileStore(fileSystem, "out").SaveIfAbsent("song.txt", [1]);

        Assert.False(saved);
        Assert.Equal([Destination], fileSystem.Files.Keys);
        Assert.Equal([9], fileSystem.Files[Destination]);
    }

    [Fact]
    public void SaveIfAbsent_removes_a_partial_file_when_writing_fails()
    {
        var fileSystem = new FakeFileSystem { FailWrite = true };
        var store = new LyricFileStore(fileSystem, "out");

        Assert.Throws<IOException>(() => store.SaveIfAbsent("song.txt", [1]));
        Assert.Empty(fileSystem.Files);
    }
}
