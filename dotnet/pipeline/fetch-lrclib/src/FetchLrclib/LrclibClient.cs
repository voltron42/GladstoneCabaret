namespace FetchLrclib;

/// <summary>A null status means the request never completed.</summary>
public sealed record FetchResponse(int? Status, byte[] Body, bool HasRetryAfter)
{
    public static FetchResponse NetworkFailure { get; } = new(null, [], false);
}

public interface ILrclibClient
{
    Task<FetchResponse> GetRecordAsync(long recordId);
}

/// <summary>The supplied HttpClient provides the base address, timeout and headers.</summary>
public sealed class LrclibClient(HttpClient httpClient) : ILrclibClient
{
    public const string UserAgent = "fetch-lrclib-poc";

    public static Uri BaseAddress { get; } = new("https://lrclib.net/");

    public static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);

    public static HttpClient CreateHttpClient(HttpMessageHandler handler, Uri baseAddress, TimeSpan timeout)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = baseAddress,
            Timeout = timeout,
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return httpClient;
    }

    public async Task<FetchResponse> GetRecordAsync(long recordId)
    {
        try
        {
            using var response = await httpClient.GetAsync(new Uri($"api/get/{recordId}", UriKind.Relative));
            var body = await response.Content.ReadAsByteArrayAsync();
            return new FetchResponse((int)response.StatusCode, body, response.Headers.Contains("Retry-After"));
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            return FetchResponse.NetworkFailure;
        }
    }
}
