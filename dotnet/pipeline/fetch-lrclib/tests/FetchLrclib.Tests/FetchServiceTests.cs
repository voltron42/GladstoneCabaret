using System.Text;

namespace FetchLrclib.Tests;

public class FetchServiceTests
{
    private static readonly Song First = new("A", "One", 1);
    private static readonly Song Second = new("B", "Two", 2);

    private static async Task<SongResult> FetchOneAsync(FetchResponse response, FakeStore? store = null) =>
        (await new FetchService(new FakeClient(response), store ?? new FakeStore()).FetchAllAsync([First]))[0];

    [Fact]
    public async Task A_record_with_line_times_saves_three_files()
    {
        var store = new FakeStore();
        var response = Records.Usable(synced: "\"timed\"");

        var result = await FetchOneAsync(response, store);

        Assert.Equal(new SongResult(First, Outcome.Go, "", true, 3, 0), result);
        Assert.Equal(["Artist - Track.json", "Artist - Track.txt", "Artist - Track.lrc"], store.Files.Keys);
        Assert.Equal(response.Body, store.Files["Artist - Track.json"]);
        Assert.Equal(Encoding.UTF8.GetBytes("made up line"), store.Files["Artist - Track.txt"]);
        Assert.Equal(Encoding.UTF8.GetBytes("timed"), store.Files["Artist - Track.lrc"]);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\" \"")]
    public async Task A_record_without_line_times_is_still_a_go(string synced)
    {
        var result = await FetchOneAsync(Records.Usable(synced: synced));

        Assert.Equal(new SongResult(First, Outcome.Go, "", false, 2, 0), result);
    }

    [Fact]
    public async Task Existing_files_are_counted_as_kept()
    {
        var store = new FakeStore { Files = { ["Artist - Track.txt"] = [7] } };

        var result = await FetchOneAsync(Records.Usable(), store);

        Assert.Equal(new SongResult(First, Outcome.Go, "", false, 1, 1), result);
    }

    [Theory]
    [InlineData("\"words\"", "true", "instrumental")]
    [InlineData("null", "false", "no plain lyrics")]
    [InlineData("\"  \"", "false", "no plain lyrics")]
    public async Task A_record_without_usable_lyrics_is_a_no_go_and_saves_nothing(
        string plain, string instrumental, string reason)
    {
        var store = new FakeStore();

        var result = await FetchOneAsync(Records.Usable(plain: plain, instrumental: instrumental), store);

        Assert.Equal(new SongResult(First, Outcome.NoGo, reason), result);
        Assert.Empty(store.Files);
    }

    [Theory]
    [InlineData(null, "network failure")]
    [InlineData(404, "record not found")]
    [InlineData(429, "rate limited")]
    [InlineData(500, "service error: HTTP 500")]
    [InlineData(200, "invalid response")]
    public async Task Failed_requests_are_errors(int? status, string reason)
    {
        var result = await FetchOneAsync(new FetchResponse(status, [], false));

        Assert.Equal(new SongResult(First, Outcome.Error, reason), result);
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public async Task A_failed_write_is_an_error(Type failure)
    {
        var store = new FakeStore { Failure = (Exception)Activator.CreateInstance(failure)! };

        var result = await FetchOneAsync(Records.Usable(), store);

        Assert.Equal(new SongResult(First, Outcome.Error, "file write failed"), result);
    }

    [Theory]
    [InlineData(429, false)]
    [InlineData(503, true)]
    public async Task A_request_to_wait_stops_further_requests(int status, bool hasRetryAfter)
    {
        var client = new FakeClient(new FetchResponse(status, [], hasRetryAfter));

        var results = await new FetchService(client, new FakeStore()).FetchAllAsync([First, Second]);

        Assert.Equal([1L], client.RequestedIds);
        Assert.Equal("not attempted: service asked us to wait", results[1].Reason);
        Assert.Equal(Outcome.Error, results[1].Outcome);
    }

    [Theory]
    [InlineData(503, false)]
    [InlineData(200, true)]
    public async Task Other_responses_do_not_stop_later_requests(int status, bool hasRetryAfter)
    {
        var client = new FakeClient(new FetchResponse(status, [], hasRetryAfter), Records.Usable(id: 2));

        var results = await new FetchService(client, new FakeStore()).FetchAllAsync([First, Second]);

        Assert.Equal([1L, 2L], client.RequestedIds);
        Assert.Equal(Outcome.Go, results[1].Outcome);
    }
}
