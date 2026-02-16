using System;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CUE4Parse.Utils;

internal static class HttpUtils
{
    internal static HttpClient DownloadClient { get; } = CreateDownloadClient();

    private static HttpClient CreateDownloadClient()
    {
        var client = new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
            nameof(CUE4Parse),
            typeof(HttpUtils).Assembly.GetName().Version?.ToString() ?? "1.0.0"));

        return client;
    }
}
