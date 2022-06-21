using System.Text;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FAssetRegistryExportPath
    {
        public readonly FTopLevelAssetPath ClassPath;
        public readonly FName Class;
        public readonly FName Object;
        public readonly FName Package;

        public FAssetRegistryExportPath(FAssetRegistryReader Ar)
        {
            if (Ar.Game < EGame.GAME_UE5_1)
            {
                Class = Ar.ReadFName();
            }
            else
            {
                ClassPath = new FTopLevelAssetPath(Ar);
            }
            
            Object = Ar.ReadFName();
            Package = Ar.ReadFName();
        }

        public FAssetRegistryExportPath(FNameEntrySerialized classs, FNameEntrySerialized objectt, FNameEntrySerialized package)
        {
            Class = new FName(classs.Name);
            Object = new FName(objectt.Name);
            Package = new FName(package.Name);
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!Class.IsNone)
                sb.Append(Class.Text + "'");
            sb.Append(Package.Text);
            if (!Object.IsNone)
                sb.Append('.' + Object.Text);
            if (!Class.IsNone)
                sb.Append("'");
            return sb.ToString();
        }
    }
}