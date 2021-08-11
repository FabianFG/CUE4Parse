using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO.Objects
{
    public readonly struct FExportMapEntry
    {
        public const int Size = 72;

        public readonly ulong CookedSerialOffset;
        public readonly ulong CookedSerialSize;
        public readonly FMappedName ObjectName;
        public readonly FPackageObjectIndex OuterIndex;
        public readonly FPackageObjectIndex ClassIndex;
        public readonly FPackageObjectIndex SuperIndex;
        public readonly FPackageObjectIndex TemplateIndex;
        public readonly FPackageObjectIndex GlobalImportIndex;
        public readonly uint ExportHash;
        public readonly EObjectFlags ObjectFlags;
        public readonly byte FilterFlags; // EExportFilterFlags: client/server flags

        public FExportMapEntry(FArchive Ar)
        {
            CookedSerialOffset = Ar.Read<ulong>();
            CookedSerialSize = Ar.Read<ulong>();
            ObjectName = Ar.Read<FMappedName>();
            OuterIndex = Ar.Read<FPackageObjectIndex>();
            ClassIndex = Ar.Read<FPackageObjectIndex>();
            SuperIndex = Ar.Read<FPackageObjectIndex>();
            TemplateIndex = Ar.Read<FPackageObjectIndex>();
            if (Ar.Game >= EGame.GAME_UE5_0) // CL 17014898
            {
                GlobalImportIndex = new FPackageObjectIndex(FPackageObjectIndex.Invalid);
                ExportHash = Ar.Read<uint>();
            }
            else
            {
                GlobalImportIndex = Ar.Read<FPackageObjectIndex>();
                ExportHash = uint.MaxValue;
            }

            ObjectFlags = Ar.Read<EObjectFlags>();
            FilterFlags = Ar.Read<byte>();
            Ar.Position = Ar.Position.Align(4);
        }

        public static int GetStructSize(FArchive Ar)
        {
            return (2 * 8 + 2 * 4 + 4 * 8 + (Ar.Game >= EGame.GAME_UE5_0 ? 4 : 8) + 4 + 1).Align(4);
        }
    }
}