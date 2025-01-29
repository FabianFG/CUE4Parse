using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.VirtualFileSystem
{
    public abstract partial class AbstractVfsReader : IVfsReader
    {
        protected static readonly ILogger log = Log.ForContext<AbstractVfsReader>();

        public string Path { get; }
        public string Name { get; }
        public long ReadOrder { get; private set; }

        public IReadOnlyDictionary<string, GameFile> Files { get; protected set; }
        public virtual int FileCount => Files.Count;


        public abstract string MountPoint { get; protected set; }
        public abstract bool HasDirectoryIndex { get; }

        public bool IsConcurrent { get; set; } = false;
        public bool IsMounted { get; } = false;

        public VersionContainer Versions { get; set; }
        public EGame Game
        {
            get => Versions.Game;
            set => Versions.Game = value;
        }
        public FPackageFileVersion Ver
        {
            get => Versions.Ver;
            set => Versions.Ver = value;
        }

        protected AbstractVfsReader(string path, VersionContainer versions)
        {
            Path = path;
            Name = path.Replace('\\', '/').SubstringAfterLast('/');
            Versions = versions;
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
                if (Globals.LogVfsMounts)
                {
                    log.Warning($"\"{Name}\" has strange mount point \"{mountPoint}\", mounting to root");
                }

                mountPoint = "/";
            }

            mountPoint = mountPoint[1..];
            VerifyReadOrder();
        }

        private void VerifyReadOrder()
        {
            ReadOrder = GetPakOrderFromPakFilePath();
            if (!Name.EndsWith("_P.pak") && !Name.EndsWith("_P.utoc") && !Name.EndsWith("_P.o.utoc"))
                return;

            var chunkVersionNumber = 1u;
            var versionEndIndex = Name.LastIndexOf('_');
            if (versionEndIndex != -1 && versionEndIndex > 0)
            {
                var versionStartIndex = Name.LastIndexOf('_', versionEndIndex - 1);
                if (versionStartIndex != -1)
                {
                    versionStartIndex++;
                    var versionString = Name.Substring(versionStartIndex, versionEndIndex - versionStartIndex);
                    if (int.TryParse(versionString, out var chunkVersionSigned) && chunkVersionSigned >= 1)
                    {
                        // Increment by one so that the first patch file still gets more priority than the base pak file
                        chunkVersionNumber = (uint)chunkVersionSigned + 1;
                    }
                }
            }
            ReadOrder += 100 * chunkVersionNumber;
        }

        private int GetPakOrderFromPakFilePath()
        {
            // if (Path.StartsWith($"{FPaths.ProjectContentDir()}Paks/{FApp.GetProjectName()}-"))
            // {
            //     // ProjectName/Content/Paks/ProjectName-
            //     return 4;
            // }
            // if (Path.StartsWith(FPaths.ProjectContentDir()))
            // {
            //     // ProjectName/Content/
            //     return 3;
            // }
            // if (Path.StartsWith(FPaths.EngineContentDir()))
            // {
            //     // Engine/Content/
            //     return 2;
            // }
            // if (Path.StartsWith(FPaths.ProjectSavedDir()))
            // {
            //     // %LocalAppData%/ProjectName/Saved/
            //     return 1;
            // }
            return 3;
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
            if (mountPointLength < 0)
            {
                // UTF16
                reader.Seek(-(mountPointLength - 1) * 2, SeekOrigin.Current);
                return reader.Read<short>() == 0;
            }

            // UTF8
            reader.Seek(mountPointLength - 1, SeekOrigin.Current);
            return reader.Read<byte>() == 0;
        }

        public abstract void Dispose();

        public override string ToString() => Path;
    }
}
