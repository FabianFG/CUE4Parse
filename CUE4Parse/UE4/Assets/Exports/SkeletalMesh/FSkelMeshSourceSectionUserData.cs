using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FSkelMeshSourceSectionUserData
{
    public bool bRecomputeTangent;
    public ESkinVertexColorChannel RecomputeTangentsVertexMaskChannel;
    public bool bCastShadow;
    public bool bVisibleInRayTracing;
    public short CorrespondClothAssetIndex;
    public FClothingSectionData ClothingData;
    public bool bDisabled;
    public int GenerateUpToLodIndex;

    public FSkelMeshSourceSectionUserData(FAssetArchive Ar)
    {
        var stripFlags = new FStripDataFlags(Ar);

        if (stripFlags.IsEditorDataStripped())
            return;

        bRecomputeTangent = Ar.ReadBoolean();

        if (FRecomputeTangentCustomVersion.Get(Ar) >= FRecomputeTangentCustomVersion.Type.RecomputeTangentVertexColorMask)
        {
            RecomputeTangentsVertexMaskChannel = (ESkinVertexColorChannel)Ar.ReadByte();
        }
        else
        {
            RecomputeTangentsVertexMaskChannel = ESkinVertexColorChannel.None;
        }

        bCastShadow = Ar.ReadBoolean();

        if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.SkelMeshSectionVisibleInRayTracingFlagAdded)
        {
            bVisibleInRayTracing = Ar.ReadBoolean();
        }
        else
        {
            bVisibleInRayTracing = true;
        }

        bDisabled = Ar.ReadBoolean();
        GenerateUpToLodIndex = Ar.Read<int>();
        CorrespondClothAssetIndex = Ar.Read<short>();
        ClothingData = Ar.Read<FClothingSectionData>();
    }
}
