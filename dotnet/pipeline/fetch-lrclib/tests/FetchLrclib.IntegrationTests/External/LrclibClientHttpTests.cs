using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FetchLrclib.IntegrationTests.External;

public class LrclibClientHttpTests
{
    private static async Task<FetchResponse> GetAsync(Uri baseAddress, TimeSpan timeout, long recordId = 1)
    {
        using var handler = new HttpClientHandler();
        using var httpClient = LrclibClient.CreateHttpClient(handler, baseAddress, timeout);
        return await new LrclibClient(httpClient).GetRecordAsync(recordId);
    }

    private static (TcpListener Listener, Uri Address) Listen()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return (listener, new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/"));
    }

    [Fact]
    public async Task Reads_status_body_and_retry_after_over_real_http()
    {
        var (listener, address) = Listen();
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync();
            var stream = connection.GetStream();
            _ = await stream.ReadAsync(new byte[4096]);
            await stream.WriteAsync(Encoding.ASCII.GetBytes(
                "HTTP/1.1 503 Busy\r\nRetry-After: 5\r\nContent-Length: 2\r\nConnection: close\r\n\r\nno"));
        });

        var response = await GetAsync(address, TimeSpan.FromSeconds(5));
        await server;
        listener.Stop();

        Assert.Equal(503, response.Status);
        Assert.True(response.HasRetryAfter);
        Assert.Equal("no"u8.ToArray(), response.Body);
    }

    [Fact]
    public async Task A_server_that_never_answers_times_out_as_network_failure()
    {
        var (listener, address) = Listen();

        var response = await GetAsync(address, TimeSpan.FromMilliseconds(500));
        listener.Stop();

        Assert.Equal(FetchResponse.NetworkFailure, response);
    }

    /// <summary>Checks the real service still matches what our stand-ins assume. Opt in with LRCLIB_LIVE=1.</summary>
    [Fact]
    public async Task Live_record_is_usable()
    {
        if (Environment.GetEnvironmentVariable("LRCLIB_LIVE") != "1")
        {
            return;
        }

        var response = await GetAsync(LrclibClient.BaseAddress, LrclibClient.Timeout, 16012238);

        Assert.Equal(200, response.Status);
        Assert.NotNull(LyricRecord.TryParse(response.Body, 16012238));
    }
}
