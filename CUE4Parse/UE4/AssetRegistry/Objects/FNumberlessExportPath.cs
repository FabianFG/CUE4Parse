using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FNumberlessExportPath
    {
        public readonly uint Class;
        public readonly uint ClassPackage;
        public readonly uint ClassObject;
        public readonly uint Object;
        public readonly uint Package;
        public readonly FNameEntrySerialized[] Names;

        public FNumberlessExportPath(FAssetRegistryReader Ar)
        {
            if (Ar.Game < EGame.GAME_UE5_1)
            {
                Class = Ar.Read<uint>();
            }
            else
            {
                ClassPackage = Ar.Read<uint>();
                ClassObject = Ar.Read<uint>();
            }
            
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