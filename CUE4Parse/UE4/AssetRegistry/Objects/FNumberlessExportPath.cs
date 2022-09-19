using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FNumberlessExportPath
    {
        public readonly uint ClassPackage;
        public readonly uint ClassObject;
        public readonly uint Object;
        public readonly uint Package;
        public readonly FNameEntrySerialized[] Names;

        public FNumberlessExportPath(FAssetRegistryArchive Ar)
        {
            if (Ar.Header.Version >= FAssetRegistryVersionType.ClassPaths)
            {
                ClassPackage = Ar.Read<uint>();
                ClassObject = Ar.Read<uint>();
            }
            else
            {
                ClassObject = Ar.Read<uint>();
            }

            Object = Ar.Read<uint>();
            Package = Ar.Read<uint>();
            Names = Ar.NameMap;
        }

        public override string ToString()
        {
            return new FAssetRegistryExportPath(Names[ClassObject], Names[Object], Names[Package]).ToString();
        }
    }
}