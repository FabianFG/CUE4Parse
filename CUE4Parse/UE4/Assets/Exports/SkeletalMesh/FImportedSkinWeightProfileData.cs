using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FImportedSkinWeightProfileData
{
    public FSkinWeightInfo[] SkinWeights;
    public FVertInfluence[] SourceModelInfluences;

    public FImportedSkinWeightProfileData(FAssetArchive Ar)
    {
        var SkinWeights = Ar.ReadArray(() => FRawSkinWeight.Serialize(Ar));
        var SourceModelInfluences = Ar.ReadArray<FVertInfluence>();
    }
}
