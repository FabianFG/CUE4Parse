using System;
using System.Linq;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.FileProvider
{
    public abstract class GameFile
    {
        public static readonly string[] Ue4PackageExtensions = { "uasset", "umap" };
        public static readonly string[] Ue4KnownExtensions = { "uasset", "umap", "uexp", "ubulk", "uptnl" };

        protected GameFile() { }
        protected GameFile(string path, long size)
        {
            Path = path;
            Size = size;
        }
        
        public string Path { get; protected set; }
        public long Size { get; protected set; }

        public string PathWithoutExtension => Path.SubstringBeforeLast('.');
        public string Name => Path.SubstringAfterLast('/');
        public string NameWithoutExtension => Name.SubstringBeforeLast('.');
        public string Extension => Path.SubstringAfterLast('.');
        
        public bool IsUE4Package => Ue4PackageExtensions.Contains(Extension, StringComparer.OrdinalIgnoreCase);

        public abstract byte[] Read();
        public abstract FArchive CreateReader();

        public override string ToString() => Path;
    }
}