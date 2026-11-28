namespace FetchLrclib.Tests;

public class SongListTests
{
    [Fact]
    public void Parse_reads_songs_and_ignores_other_properties()
    {
        var songs = SongList.Parse("""[{"artist":"A","songTitle":"T","lrclibRecordId":5,"category":"x"}]""");

        Assert.Equal([new Song("A", "T", 5)], songs);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("[1]")]
    [InlineData("""[{"songTitle":"T","lrclibRecordId":5}]""")]
    [InlineData("""[{"artist":" ","songTitle":"T","lrclibRecordId":5}]""")]
    [InlineData("""[{"artist":"A","songTitle":7,"lrclibRecordId":5}]""")]
    [InlineData("""[{"artist":"A","songTitle":"T"}]""")]
    [InlineData("""[{"artist":"A","songTitle":"T","lrclibRecordId":0}]""")]
    [InlineData("""[{"artist":"A","songTitle":"T","lrclibRecordId":"5"}]""")]
    [InlineData("""[{"artist":"A","songTitle":"T","lrclibRecordId":1.5}]""")]
    public void Parse_rejects_invalid_lists(string json) =>
        Assert.Throws<SongListException>(() => SongList.Parse(json));
}
