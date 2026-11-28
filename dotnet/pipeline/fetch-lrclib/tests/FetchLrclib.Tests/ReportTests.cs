namespace FetchLrclib.Tests;

public class ReportTests
{
    private static readonly Song Song = new("A", "One", 1);
    private static readonly SongResult Go = new(Song, Outcome.Go, "", true, 3, 0);
    private static readonly SongResult NoGo = new(Song, Outcome.NoGo, "instrumental");
    private static readonly SongResult Error = new(Song, Outcome.Error, "network failure");

    [Fact]
    public void Format_writes_one_line_per_song_and_a_summary()
    {
        var expected =
            "GO     A - One (line times: yes; saved: 3, kept: 0)\n"
            + "GO     A - One (line times: no; saved: 0, kept: 2)\n"
            + "NO-GO  A - One (instrumental)\n"
            + "ERROR  A - One (network failure)\n"
            + "Summary: go=2, no-go=1, error=1\n";

        var report = Report.Format([Go, new SongResult(Song, Outcome.Go, "", false, 0, 2), NoGo, Error]);

        Assert.Equal(expected, report);
    }

    [Fact]
    public void ExitCode_reflects_the_worst_outcome()
    {
        Assert.Equal(0, Report.ExitCode([Go]));
        Assert.Equal(1, Report.ExitCode([Go, NoGo]));
        Assert.Equal(2, Report.ExitCode([Go, NoGo, Error]));
    }
}
