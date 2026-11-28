using System.Net;

namespace FetchLrclib.Tests;

public class LrclibClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    private static async Task<FetchResponse> GetAsync(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        using var handler = new StubHandler(respond);
        using var httpClient = LrclibClient.CreateHttpClient(handler, LrclibClient.BaseAddress, LrclibClient.Timeout);
        return await new LrclibClient(httpClient).GetRecordAsync(42);
    }

    [Fact]
    public async Task GetRecordAsync_requests_the_record_with_a_generic_user_agent()
    {
        HttpRequestMessage? sent = null;

        var response = await GetAsync(request =>
        {
            sent = request;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2]) };
        });

        Assert.Equal("https://lrclib.net/api/get/42", sent!.RequestUri!.AbsoluteUri);
        Assert.Equal("fetch-lrclib-poc", sent.Headers.UserAgent.ToString());
        Assert.Equal(200, response.Status);
        Assert.Equal([1, 2], response.Body);
        Assert.False(response.HasRetryAfter);
    }

    [Fact]
    public async Task GetRecordAsync_reports_retry_after()
    {
        var response = await GetAsync(_ =>
        {
            var message = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            message.Headers.Add("Retry-After", "5");
            return message;
        });

        Assert.Equal(503, response.Status);
        Assert.True(response.HasRetryAfter);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(IOException))]
    public async Task GetRecordAsync_turns_failures_into_network_failure(Type failure)
    {
        var response = await GetAsync(_ => throw (Exception)Activator.CreateInstance(failure)!);

        Assert.Equal(FetchResponse.NetworkFailure, response);
    }

    [Fact]
    public void Timeout_is_thirty_seconds() => Assert.Equal(TimeSpan.FromSeconds(30), LrclibClient.Timeout);
}
