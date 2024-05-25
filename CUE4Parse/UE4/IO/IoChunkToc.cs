using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO
{
    public class IoChunkToc
    {
        public readonly FOnDemandTocHeader Header;
        public readonly FTocMeta Meta;
        public readonly FOnDemandTocContainerEntry[] Containers;
        public readonly FOnDemandTocAdditionalFile[] AdditionalFiles;
        public readonly FOnDemandTocTagSet[] TagSets;

        public IoChunkToc(string file) : this(new FileInfo(file)) { }
        public IoChunkToc(FileInfo file) : this(new FByteArchive(file.FullName, File.ReadAllBytes(file.FullName))) { }
        public IoChunkToc(FArchive Ar)
        {
            Header = new FOnDemandTocHeader(Ar);

            if (Header.Version >= EOnDemandTocVersion.Meta)
                Meta = new FTocMeta(Ar);

            Containers = Ar.ReadArray(() => new FOnDemandTocContainerEntry(Ar, Header.Version));
            
            if (Header.Version >= EOnDemandTocVersion.AdditionalFiles)
                AdditionalFiles = Ar.ReadArray(() => new FOnDemandTocAdditionalFile(Ar));
            
            if (Header.Version >= EOnDemandTocVersion.TagSets)
                TagSets = Ar.ReadArray(() => new FOnDemandTocTagSet(Ar));
        }
    }

    public class IoStoreOnDemandOptions
    {
        public Uri ChunkBaseUri { get; set; }
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

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_options.ChunkBaseUri, url));
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
}
