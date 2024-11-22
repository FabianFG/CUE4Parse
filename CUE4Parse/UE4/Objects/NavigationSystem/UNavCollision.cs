using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.NavigationSystem;

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

        _ = Ar.Read<FGuid>(); // Zeroed GUID, unused
        var bCooked = Ar.ReadBoolean();

        var bGatherConvexGeometry = GetOrDefault<bool>("bGatherConvexGeometry");
        var boxCollision = GetOrDefault<object[]>("BoxCollision", []);
        var cylinderCollision = GetOrDefault<object[]>("CylinderCollision", []);
        bool bUseConvexCollisionVer3 = bGatherConvexGeometry || (cylinderCollision.Length == 0 && boxCollision.Length == 0);
        bool bUseConvexCollision = bGatherConvexGeometry || boxCollision.Length > 0 || cylinderCollision.Length > 0;
        bool bProcessCookedData = version >= Consts.ShapeGeoExport ? bUseConvexCollision : bUseConvexCollisionVer3;

        if (bCooked && bProcessCookedData)
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
