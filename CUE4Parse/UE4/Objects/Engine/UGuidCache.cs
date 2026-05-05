using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UGuidCache : Assets.Exports.UObject
    {
        public Dictionary<FName, FGuid> PackageGuidMap;
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            PackageGuidMap = Ar.ReadMap(Ar.ReadFName, () => Ar.Read<FGuid>());
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (PackageGuidMap.Count > 0)
            {
                writer.WritePropertyName("PackageGuidMap");
                serializer.Serialize(writer, PackageGuidMap);
            }
        }
    }
}
