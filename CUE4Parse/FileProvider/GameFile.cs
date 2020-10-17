using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider
{
    public abstract class GameFile
    {
        public static readonly string[] Ue4PackageExtensions = { "uasset", "umap" };
        public static readonly string[] Ue4KnownExtensions = { "uasset", "umap", "uexp", "ubulk", "uptnl" };

        protected GameFile() { }
        protected GameFile(string path, long size, UE4Version ver, EGame game)
        {
            Path = path;
            Size = size;
            Ver = ver;
            Game = game;
        }
        
        public UE4Version Ver { get; protected set; }
        public EGame Game { get; protected set; }
        public string Path { get; protected set; }
        public long Size { get; protected set; }

        public string PathWithoutExtension => Path.SubstringBeforeLast('.');
        public string Name => Path.SubstringAfterLast('/');
        public string NameWithoutExtension => Name.SubstringBeforeLast('.');
        public string Extension => Path.SubstringAfterLast('.');
        
        public bool IsUE4Package => Ue4PackageExtensions.Contains(Extension, StringComparer.OrdinalIgnoreCase);
        
        public abstract byte[] Read();
        public abstract FArchive CreateReader();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryRead(out byte[] data)
        {
            try
            {
                data = Read();
                return true;
            }
            catch
            {
                data = default;
                return false;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool TryCreateReader(out FArchive reader)
        {
            try
            {
                reader = CreateReader();
                return true;
            }
            catch
            {
                reader = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<byte[]> ReadAsync() => await Task.Run(Read); // No ConfigureAwait(false) here since the context is needed handling exceptions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<FArchive> CreateReaderAsync() => await Task.Run(CreateReader);  // No ConfigureAwait(false) here since the context is needed handling exceptions
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<byte[]?> TryReadAsync() => await Task.Run(() =>
        {
            TryRead(out var data);
            return data;
        }).ConfigureAwait(false);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<FArchive?> TryCreateReaderAsync() => await Task.Run(() =>
        {
            TryCreateReader(out var reader);
            return reader;
        }).ConfigureAwait(false);

        public override string ToString() => Path;
    }
}