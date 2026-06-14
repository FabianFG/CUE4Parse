using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.IO.Objects.OnDemand;
using CUE4Parse.UE4.IO.Objects.OnDemand.V2;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO;

public class IoChunkToc
{
    public IOnDemandToc OnDemandToc;

    public IoChunkToc(string file, VersionContainer versions) : this(new FileInfo(file), versions) { }
    public IoChunkToc(FileInfo file, VersionContainer versions) : this(new FByteArchive(file.FullName, File.ReadAllBytes(file.FullName), versions)) { }
    public IoChunkToc(FArchive Ar)
    {
        var bIsLegacy = Ar.Read<ulong>() == 0x6f6e64656d616e64;
        Ar.Position = 0;

        OnDemandToc = bIsLegacy ? new Objects.OnDemand.V1.FOnDemandToc(Ar) : new FOnDemandToc(Ar);
    }
}

public class IoStoreOnDemandOptions
{
    public HttpClient? DownloaderClient { get; set; }
    public required Uri ChunkHostUri { get; init; }
    public DirectoryInfo? ChunkCacheDirectory { get; init; }
    public AuthenticationHeaderValue? Authorization { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool UseAuth => Authorization != null;
}

public class IoStoreOnDemandDownloader : IDisposable
{
    private readonly IoStoreOnDemandOptions _options;
    private readonly HttpClient _client;
    private readonly bool _disposeClient;

    public IoStoreOnDemandDownloader(IoStoreOnDemandOptions options)
    {
        _options = options;
        _client = options.DownloaderClient ?? new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None
        }) { Timeout = options.Timeout };
        _disposeClient = options.DownloaderClient is null;
    }

    public async Task<Stream> Download(string url, long position = 0)
    {
        var cachePath = _options.ChunkCacheDirectory is not null && _options.ChunkCacheDirectory.Exists
            ? Path.Combine(_options.ChunkCacheDirectory.FullName, url.SubstringAfterLast('/'))
            : null;
        if (cachePath is not null && File.Exists(cachePath))
        {
            var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Position = position;
            return fs;
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.ChunkHostUri, url));
        if (_options.UseAuth) requestMessage.Headers.Authorization = _options.Authorization;
        using var response = await _client.SendAsync(requestMessage).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var outData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        var outStream = new MemoryStream(outData, 0, outData.Length, false, true);

        if (cachePath is not null)
        {
            await using var cacheFs = new FileStream(cachePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            await outStream.CopyToAsync(cacheFs).ConfigureAwait(false);
        }

        outStream.Position = position;
        return outStream;
    }

    public void Dispose()
    {
        if (_disposeClient)
            _client.Dispose();
    }
}
