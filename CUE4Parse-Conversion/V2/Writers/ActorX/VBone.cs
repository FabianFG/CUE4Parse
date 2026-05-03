using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.ActorX;

public class VBone
{
    public string Name;
    public uint Flags;
    public int NumChildren;
    public int ParentIndex;
    public VJointPosPsk BonePos;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(Name, 64);
        Ar.Write(Flags);
        Ar.Write(NumChildren);
        Ar.Write(ParentIndex);
        BonePos.Serialize(Ar);
    }
}
