using System.Text;
using FetchLrclib;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

using var handler = new HttpClientHandler();
using var httpClient = LrclibClient.CreateHttpClient(handler, LrclibClient.BaseAddress, LrclibClient.Timeout);
var dependencies = new Dependencies(
    File.ReadAllText,
    new LrclibClient(httpClient),
    directory => new LyricFileStore(new PhysicalFileSystem(), directory));

return await App.RunAsync(args, dependencies, Console.Out, Console.Error);
