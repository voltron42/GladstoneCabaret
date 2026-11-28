using System.Text;

namespace FetchLrclib;

public static class Report
{
    public static string Format(IReadOnlyList<SongResult> results)
    {
        var report = new StringBuilder();
        foreach (var result in results)
        {
            report.Append(FormatLine(result)).Append('\n');
        }

        var goCount = Count(results, Outcome.Go);
        var noGoCount = Count(results, Outcome.NoGo);
        var errorCount = Count(results, Outcome.Error);
        var summary = $"Summary: go={goCount}, no-go={noGoCount}, error={errorCount}\n";
        return report.Append(summary).ToString();
    }

    public static int ExitCode(IReadOnlyList<SongResult> results)
    {
        if (Count(results, Outcome.Error) > 0)
        {
            return 2;
        }

        return Count(results, Outcome.NoGo) > 0 ? 1 : 0;
    }

    private static int Count(IReadOnlyList<SongResult> results, Outcome outcome) =>
        results.Count(result => result.Outcome == outcome);

    private static string FormatLine(SongResult result)
    {
        var label = result.Outcome switch
        {
            Outcome.Go => "GO",
            Outcome.NoGo => "NO-GO",
            _ => "ERROR",
        };
        var lineTimes = result.HasLineTimes ? "yes" : "no";
        var detail = result.Outcome == Outcome.Go
            ? $"line times: {lineTimes}; saved: {result.SavedCount}, kept: {result.KeptCount}"
            : result.Reason;
        return $"{label,-7}{result.Song.Artist} - {result.Song.SongTitle} ({detail})";
    }
}
