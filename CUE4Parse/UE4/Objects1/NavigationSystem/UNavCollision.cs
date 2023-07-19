using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.NavigationSystem
{
    public class UNavCollision : Assets.Exports.UObject
    {
        public FFormatContainer? CookedFormatData;
        public FPackageIndex? AreaClass; // UNavArea

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var startPos = Ar.Position;
            int version;
            var myMagic = Ar.Read<uint>();

            if (myMagic != Consts.Magic)
            {
                version = Consts.Initial;
                Ar.Position = startPos;
            }
            else
            {
                version = Ar.Read<int>();
            }

            var _ = Ar.Read<FGuid>(); // Zeroed GUID, unused
            var bCooked = Ar.ReadBoolean();
            
            if (bCooked)
                CookedFormatData = new FFormatContainer(Ar);

            if (version >= Consts.AreaClass)
                AreaClass = new FPackageIndex(Ar);
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (CookedFormatData != null)
            {
                writer.WritePropertyName("CookedFormatData");
                serializer.Serialize(writer, CookedFormatData);
            }

            if (AreaClass is { IsNull: false })
            {
                writer.WritePropertyName("AreaClass");
                serializer.Serialize(writer, AreaClass);
            }
        }

        public class Consts
        {
            public const int Initial = 1;
            public const int AreaClass = 2;
            public const int ConvexTransforms = 3;
            public const int ShapeGeoExport = 4;
            public const int Latest = ShapeGeoExport;
            public const uint Magic = 0xA237F237;
        }
    }
}
