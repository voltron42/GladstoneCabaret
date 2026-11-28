namespace FetchLrclib.Tests;

public class LyricRecordTests
{
    [Fact]
    public void TryParse_reads_a_usable_record()
    {
        var record = LyricRecord.TryParse(Records.Usable(synced: "\"[00:01.00] made up\"").Body, 1);

        Assert.Equal(new LyricRecord("Artist", "Track", false, "made up line", "[00:01.00] made up"), record);
    }

    [Fact]
    public void TryParse_allows_absent_lyrics()
    {
        var body = Records.Bytes("""{"id":1,"artistName":"A","trackName":"T","instrumental":true}""");

        Assert.Equal(new LyricRecord("A", "T", true, null, null), LyricRecord.TryParse(body, 1));
    }

    [Theory]
    [InlineData("<html>")]
    [InlineData("[]")]
    [InlineData("""{"artistName":"A","trackName":"T","instrumental":false}""")]
    [InlineData("""{"id":2,"artistName":"A","trackName":"T","instrumental":false}""")]
    [InlineData("""{"id":"1","artistName":"A","trackName":"T","instrumental":false}""")]
    [InlineData("""{"id":1,"trackName":"T","instrumental":false}""")]
    [InlineData("""{"id":1,"artistName":"A","trackName":"  ","instrumental":false}""")]
    [InlineData("""{"id":1,"artistName":"A","trackName":"T"}""")]
    [InlineData("""{"id":1,"artistName":"A","trackName":"T","instrumental":"no"}""")]
    [InlineData("""{"id":1,"artistName":"A","trackName":"T","instrumental":false,"plainLyrics":3}""")]
    [InlineData("""{"id":1,"artistName":"A","trackName":"T","instrumental":false,"syncedLyrics":[]}""")]
    [InlineData("""{"id":1,"artistName":"A","trackName":"T","instrumental":false,"plainLyrics":"\ud800"}""")]
    public void TryParse_rejects_unusable_bodies(string json) =>
        Assert.Null(LyricRecord.TryParse(Records.Bytes(json), 1));
}
