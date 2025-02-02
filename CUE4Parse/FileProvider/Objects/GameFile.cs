using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.FileProvider.Objects
{
    public abstract class GameFile
    {
        public static readonly string[] Ue4PackageExtensions = ["uasset", "umap"];
        public static readonly string[] Ue4KnownExtensions =
        [
            ..Ue4PackageExtensions, "uexp", "ubulk", "uptnl",
            "bin", "ini", "uplugin", "upluginmanifest", "locres", "locmeta",
        ];

        protected GameFile() { }
        protected GameFile(string path, long size)
        {
            Path = path;
            Size = size;
        }

        public abstract bool IsEncrypted { get; }
        public abstract CompressionMethod CompressionMethod { get; }
        public string Path { get; protected internal set; }
        public long Size { get; protected set; }

        public string PathWithoutExtension => Path.SubstringBeforeLast('.');
        public string Name => Path.SubstringAfterLast('/');
        public string NameWithoutExtension => Name.SubstringBeforeLast('.');
        public string Extension => Path.SubstringAfterLast('.');

        public bool IsUE4Package => Ue4PackageExtensions.Contains(Extension, StringComparer.OrdinalIgnoreCase);

        public abstract byte[] Read();
        public abstract FArchive CreateReader();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead([MaybeNullWhen(false)] out byte[] data)
        {
            try
            {
                data = Read();
            }
            catch (Exception e)
            {
                Log.Error(e, $"Couldn't read GameFile {this}");
                data = null;
            }
            return data != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCreateReader([MaybeNullWhen(false)] out FArchive reader)
        {
            try
            {
                reader = CreateReader();
            }
            catch (Exception e)
            {
                Log.Error(e, $"Couldn't create reader for GameFile {this}");
                reader = null;
            }
            return reader != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[]? SafeRead()
        {
            TryRead(out var data);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FArchive? SafeCreateReader()
        {
            TryCreateReader(out var reader);
            return reader;
        }

        // No ConfigureAwait(false) here since the context is needed handling exceptions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<byte[]> ReadAsync() => await Task.Run(Read);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<FArchive> CreateReaderAsync() => await Task.Run(CreateReader);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<byte[]?> SafeReadAsync() => await Task.Run(SafeRead);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<FArchive?> SafeCreateReaderAsync() => await Task.Run(SafeCreateReader);

        public override string ToString() => Path;
    }
}
