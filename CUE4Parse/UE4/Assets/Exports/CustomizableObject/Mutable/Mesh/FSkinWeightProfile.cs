using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;

public class FSkinWeightProfile
{
    public FName Name;
    public bool bDefaultProfile;
    public sbyte DefaultProfileFromLODIndex;
    public byte NumBoneInfluences;
    public bool bUse16BitBoneIndex;
    public bool bUse16BitBoneWeight;
    public byte[] BoneIDs = [];
    public byte[] BoneWeights = [];
    public FVertexInfo[] VertexIndexToInfluenceOffset = [];

    public FSkinWeightProfile(FMutableArchive Ar)
    {
        Name = Ar.ReadFName();
        bDefaultProfile = Ar.ReadFlag();
        DefaultProfileFromLODIndex = Ar.Read<sbyte>();
        NumBoneInfluences = Ar.Read<byte>();
        bUse16BitBoneIndex = Ar.ReadFlag();
        bUse16BitBoneWeight = Ar.ReadFlag();
        BoneIDs = Ar.ReadArray<byte>();
        BoneWeights = Ar.ReadArray<byte>();
        VertexIndexToInfluenceOffset = Ar.ReadArray<FVertexInfo>();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FVertexInfo
    {
        public uint VertexIndex;
        public uint InfluenceOffset;
    }
}
