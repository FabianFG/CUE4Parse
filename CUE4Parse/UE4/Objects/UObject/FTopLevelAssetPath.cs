using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    [JsonConverter(typeof(FTopLevelAssetPathConverter))]
    public readonly struct FTopLevelAssetPath : IUStruct
    {
        public readonly FName PackageName;
        public readonly FName AssetName;

        public FTopLevelAssetPath(FArchive Ar)
        {
            PackageName = Ar.ReadFName();
            AssetName = Ar.ReadFName();
        }

        public FTopLevelAssetPath(FName packageName, FName assetName)
        {
            PackageName = packageName;
            AssetName = assetName;
        }

        public FTopLevelAssetPath(string path)
        {
            AssetName = path.SubstringAfterLast('.');
            PackageName = path.SubstringBeforeLast('.');
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (PackageName.IsNone) return string.Empty;
            builder.Append(PackageName);
            if (!AssetName.IsNone) builder.Append('.').Append(AssetName);
            return builder.ToString();
        }
    }
}
