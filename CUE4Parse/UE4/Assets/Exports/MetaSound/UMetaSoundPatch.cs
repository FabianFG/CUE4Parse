using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

public class UMetaSoundPatch : UObject
{
    public FMetasoundFrontendDocument RootMetaSoundDocument;
    public string[] ReferencedAssetClassKeys;
    public FPackageIndex[] ReferencedAssetClassObjects;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        RootMetaSoundDocument = GetOrDefault<FMetasoundFrontendDocument>(nameof(RootMetaSoundDocument));
        ReferencedAssetClassKeys = GetOrDefault<string[]>(nameof(ReferencedAssetClassKeys), []);
        ReferencedAssetClassObjects = GetOrDefault<FPackageIndex[]>(nameof(ReferencedAssetClassObjects));
    }
}