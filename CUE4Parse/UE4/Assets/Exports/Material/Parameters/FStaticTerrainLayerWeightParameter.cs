using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Material.Parameters;

public class FStaticTerrainLayerWeightParameter : FStaticParameterBase
{
    public int WeightmapIndex;
    public bool bWeightBasedBlend;

    public FStaticTerrainLayerWeightParameter() { }

    public FStaticTerrainLayerWeightParameter(FArchive Ar) : base(Ar)
    {
        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.StaticParameterTerrainLayerWeightBlendType)
        {
            bWeightBasedBlend = Ar.ReadBoolean();
        }

        WeightmapIndex = Ar.Read<int>();
        bOverride = Ar.ReadBoolean();
        ExpressionGuid = Ar.Read<FGuid>();
    }
}
