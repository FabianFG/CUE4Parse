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
    public Uri ChunkHostUri { get; set; }
    public DirectoryInfo ChunkCacheDirectory { get; set; }
    public AuthenticationHeaderValue? Authorization { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool UseAuth => Authorization != null;
}

public class IoStoreOnDemandDownloader : IDisposable
{
    private readonly IoStoreOnDemandOptions _options;
    private readonly HttpClient _client;

    public IoStoreOnDemandDownloader(IoStoreOnDemandOptions options)
    {
        _options = options;
        _client = new HttpClient(new HttpClientHandler
        {
            UseProxy = false,
            UseCookies = false,
            CheckCertificateRevocationList = false,
            UseDefaultCredentials = false,
            AutomaticDecompression = DecompressionMethods.None
        }) { Timeout = options.Timeout };
    }

    public async Task<Stream> Download(string url)
    {
        var cachePath = _options.ChunkCacheDirectory.Exists ? Path.Combine(_options.ChunkCacheDirectory.FullName, url.SubstringAfterLast('/')) : null;
        if (cachePath != null && File.Exists(cachePath))
        {
            var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return fs;
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.ChunkHostUri, url));
        if (_options.UseAuth) requestMessage.Headers.Authorization = _options.Authorization;
        using var response = await _client.SendAsync(requestMessage).ConfigureAwait(false);
        var outData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        var outStream = new MemoryStream(outData, false);

        if (cachePath != null)
        {
            await using var cacheFs = new FileStream(cachePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            await outStream.CopyToAsync(cacheFs).ConfigureAwait(false);
        }

        outStream.Position = 0L;
        return outStream;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
