using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public partial class USkeletalMesh
{
    public FStaticLODModel[] GFPSerializeLODModels(FAssetArchive Ar)
    {
        var len = Ar.Read<int>();
        var lodModels = new FStaticLODModel[len];
        if (GetOrDefault<bool>("bIsStreamable"))
        {
            for (var i = 0; i < lodModels.Length; i++)
            {
                var bulkData = new FByteBulkData(Ar);
                if (bulkData.Header.ElementCount > 0 && bulkData.Data != null)
                {
                    using var tempAr = new FByteArchive("StaticMeshBufferReader", bulkData.Data, Ar.Versions);
                    lodModels[i] = new FStaticLODModel(tempAr, bHasVertexColors, Ar.IsFilterEditorOnly);
                }
            }
        }
        else
        {
            var additionalData = GetOrDefault<FStructFallback[]>("SkinWeightProfiles", []).Length > 0;
            for (var i = 0; i < lodModels.Length; i++)
            {
                lodModels[i] = new FStaticLODModel(Ar, bHasVertexColors);
                if (additionalData)
                {
                    if (Ar.ReadBoolean())
                    {
                        Ar.SkipBulkArrayData();
                        Ar.SkipFixedArray(5);
                        Ar.SkipFixedArray(2);
                        Ar.SkipFixedArray(8);
                    }
                    if (Ar.ReadBoolean())
                    {
                        Ar.SkipBulkArrayData();
                        Ar.SkipFixedArray(16); // Weights
                        Ar.SkipFixedArray(10);
                    }
                }
            }
        }

        return lodModels;
    }
}
