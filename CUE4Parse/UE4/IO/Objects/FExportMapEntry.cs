using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

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
            var start = Ar.Position;
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
                ExportHash = 0;
            }

            ObjectFlags = Ar.Read<EObjectFlags>();
            FilterFlags = Ar.Read<byte>();
            Ar.Position = start + Size;
        }
    }
}