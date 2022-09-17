using System;
using System.Text;
using CUE4Parse.UE4.Readers;
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

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (PackageName.IsNone) return string.Empty;
            builder.Append(PackageName);
            if (!AssetName.IsNone) builder.Append('.').Append(AssetName);
            return builder.ToString();
        }
    }

    public class FTopLevelAssetPathConverter : JsonConverter<FTopLevelAssetPath>
    {
        public override void WriteJson(JsonWriter writer, FTopLevelAssetPath value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override FTopLevelAssetPath ReadJson(JsonReader reader, Type objectType, FTopLevelAssetPath existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}