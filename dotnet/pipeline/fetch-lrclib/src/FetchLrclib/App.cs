namespace FetchLrclib;

public sealed record Dependencies(
    Func<string, string> ReadText,
    ILrclibClient Client,
    Func<string, ILyricFileStore> CreateStore);

public static class App
{
    public const int UsageExitCode = 64;
    private const string Usage = "usage: fetch-lrclib --songs <song list file> --output <directory>";

    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments, Dependencies dependencies, TextWriter output, TextWriter errorOutput)
    {
        if (!TryReadOptions(arguments, out var songsPath, out var outputDirectory))
        {
            await errorOutput.WriteAsync($"{Usage}\n");
            return UsageExitCode;
        }

        IReadOnlyList<Song> songs;
        try
        {
            songs = SongList.Parse(dependencies.ReadText(songsPath));
        }
        catch (Exception exception) when (exception is SongListException or IOException or UnauthorizedAccessException)
        {
            await errorOutput.WriteAsync($"cannot use song list: {exception.Message}\n");
            return UsageExitCode;
        }

        var service = new FetchService(dependencies.Client, dependencies.CreateStore(outputDirectory));
        var results = await service.FetchAllAsync(songs);
        await output.WriteAsync(Report.Format(results));
        return Report.ExitCode(results);
    }

    private static bool TryReadOptions(
        IReadOnlyList<string> arguments, out string songsPath, out string outputDirectory)
    {
        var options = new Dictionary<string, string>();
        for (var index = 0; index + 1 < arguments.Count; index += 2)
        {
            options[arguments[index]] = arguments[index + 1];
        }

        songsPath = options.GetValueOrDefault("--songs", "");
        outputDirectory = options.GetValueOrDefault("--output", "");
        return arguments.Count == 4 && songsPath.Length > 0 && outputDirectory.Length > 0;
    }
}
