using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.Engine;

public class UShadowMap1D : UObject
{
    /** The incident light samples for a 1D array of points. */
    float[] Samples;

    /** The GUID of the light which this shadow-map is for. */
    FGuid LightGuid;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Samples = Ar.ReadArray<float>();
        LightGuid = Ar.Read<FGuid>();
    }
}
