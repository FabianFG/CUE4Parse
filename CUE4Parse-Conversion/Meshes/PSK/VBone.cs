using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.ActorX;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class VBone : ISerializable
{
    public string Name;
    public uint Flags;
    public int NumChildren;
    public int ParentIndex;
    public VJointPosPsk BonePos;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(Name, 64);
        Ar.Write(Flags);
        Ar.Write(NumChildren);
        Ar.Write(ParentIndex);
        Ar.Serialize(BonePos);
    }
}