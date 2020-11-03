using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Vfs
{
    public abstract partial class AbstractVfsReader : IVfsReader
    {
        protected static readonly ILogger log = Log.ForContext<AbstractVfsReader>();
        public string Name { get; }
        public IReadOnlyDictionary<string, GameFile> Files { get; protected set; }
        public virtual int FileCount => Files.Count;
        
        public bool IsConcurrent { get; set; } = false;
        public bool IsMounted { get; } = false;

        public UE4Version Ver { get; set; }
        public EGame Game { get; set; }

        protected AbstractVfsReader(string name, UE4Version ver, EGame game)
        {
            Name = name;
            Ver = ver;
            Game = game;
            Files = new Dictionary<string, GameFile>();
        }

        public abstract IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false);

        public abstract byte[] Extract(VfsEntry entry);
        
        protected void ValidateMountPoint(ref string mountPoint)
        {
            var badMountPoint = !mountPoint.StartsWith("../../..");
            mountPoint = mountPoint.SubstringAfter("../../..");
            if (mountPoint[0] != '/' || ( (mountPoint.Length > 1) && (mountPoint[1] == '.') ))
                badMountPoint = true;

            if (badMountPoint)
            {
                log.Warning($"\"{Name}\" has strange mount point \"{mountPoint}\", mounting to root");
                mountPoint = "/";
            }

            mountPoint = mountPoint.Substring(1);
        }
        
        protected const int MAX_MOUNTPOINT_TEST_LENGTH = 128;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex(byte[] testBytes) => IsValidIndex(new FByteArchive(string.Empty, testBytes));
        public static bool IsValidIndex(FArchive reader)
        {
            var mountPointLength = reader.Read<int>();
            if (mountPointLength > MAX_MOUNTPOINT_TEST_LENGTH || mountPointLength < -MAX_MOUNTPOINT_TEST_LENGTH)
                return false;
            // Calculate the pos of the null terminator for this string
            // Then read the null terminator byte and check whether it is actually 0
            if (mountPointLength == 0) return reader.Read<byte>() == 0;
            else if (mountPointLength < 0)
            {
                // UTF16
                reader.Seek(-(mountPointLength - 1) * 2, SeekOrigin.Current);
                return reader.Read<short>() == 0;
            }
            else
            {
                // UTF8
                reader.Seek(mountPointLength - 1, SeekOrigin.Current);
                return reader.Read<byte>() == 0;
            }
        }

        public abstract void Dispose();

        public override string ToString() => Name;
    }
}