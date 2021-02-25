using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FNumberlessExportPath
    {
        public readonly uint Class;
        public readonly uint Object;
        public readonly uint Package;
        public readonly FNameEntrySerialized[] Names;

        public FNumberlessExportPath(FAssetRegistryReader Ar)
        {
            Class = Ar.Read<uint>();
            Object = Ar.Read<uint>();
            Package = Ar.Read<uint>();
            Names = Ar.NameMap;
        }

        public override string ToString()
        {
            return new FAssetRegistryExportPath(Names[Class], Names[Object], Names[Package]).ToString();
        }
    }
}