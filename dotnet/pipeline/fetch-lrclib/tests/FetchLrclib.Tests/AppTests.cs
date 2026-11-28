namespace FetchLrclib.Tests;

public class AppTests
{
    private const string ValidList = """[{"artist":"A","songTitle":"One","lrclibRecordId":1}]""";

    private static async Task<(int ExitCode, string Output, string ErrorOutput)> RunAsync(
        string[] arguments, Func<string, string> readText)
    {
        using var output = new StringWriter();
        using var errorOutput = new StringWriter();
        var dependencies = new Dependencies(readText, new FakeClient(Records.Usable()), _ => new FakeStore());
        var exitCode = await App.RunAsync(arguments, dependencies, output, errorOutput);
        return (exitCode, output.ToString(), errorOutput.ToString());
    }

    [Fact]
    public async Task RunAsync_accepts_options_in_either_order()
    {
        var result = await RunAsync(["--output", "out", "--songs", "list"], path =>
        {
            Assert.Equal("list", path);
            return ValidList;
        });

        Assert.Equal(0, result.ExitCode);
        Assert.EndsWith("Summary: go=1, no-go=0, error=0\n", result.Output);
        Assert.Empty(result.ErrorOutput);
    }

    [Theory]
    [InlineData]
    [InlineData("--songs", "list")]
    [InlineData("--songs", "list", "--songs", "other")]
    [InlineData("--songs", "list", "--wrong", "out")]
    [InlineData("--songs", "", "--output", "out")]
    [InlineData("--songs", "list", "--output", "out", "extra")]
    public async Task RunAsync_rejects_wrong_arguments(params string[] arguments)
    {
        var result = await RunAsync(arguments, _ => ValidList);

        Assert.Equal(64, result.ExitCode);
        Assert.StartsWith("usage:", result.ErrorOutput);
        Assert.Empty(result.Output);
    }

    [Theory]
    [InlineData(typeof(FileNotFoundException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public async Task RunAsync_rejects_an_unreadable_song_list(Type failure)
    {
        var result = await RunAsync(
            ["--songs", "list", "--output", "out"], _ => throw (Exception)Activator.CreateInstance(failure)!);

        Assert.Equal(64, result.ExitCode);
        Assert.StartsWith("cannot use song list:", result.ErrorOutput);
    }

    [Fact]
    public async Task RunAsync_rejects_an_invalid_song_list()
    {
        var result = await RunAsync(["--songs", "list", "--output", "out"], _ => "[]");

        Assert.Equal(64, result.ExitCode);
        Assert.Equal("cannot use song list: song list must be a non-empty JSON array\n", result.ErrorOutput);
    }
}
