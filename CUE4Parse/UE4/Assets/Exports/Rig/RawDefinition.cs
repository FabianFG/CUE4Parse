using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawDefinition : IRawBase
{
    public RawLODMapping LodJointMapping;
    public RawLODMapping LodBlendShapeMapping;
    public RawLODMapping LodAnimatedMapMapping;
    public RawLODMapping LodMeshMapping;
    public string[] GuiControlNames;
    public string[] RawControlNames;
    public string[] JointNames;
    public string[] BlendShapeChannelNames;
    public string[] AnimatedMapNames;
    public string[] MeshNames;
    public RawSurjectiveMapping MeshBlendShapeChannelMapping;
    public ushort[] JointHierarchy;
    public RawVector3Vector NeutralJointTranslations;
    public RawVector3Vector NeutralJointRotations;

    public RawDefinition(FArchiveBigEndian Ar)
    {
        LodJointMapping = new RawLODMapping(Ar);
        LodBlendShapeMapping = new RawLODMapping(Ar);
        LodAnimatedMapMapping = new RawLODMapping(Ar);
        LodMeshMapping = new RawLODMapping(Ar);
        GuiControlNames = Ar.ReadArray(Ar.ReadString);
        RawControlNames = Ar.ReadArray(Ar.ReadString);
        JointNames = Ar.ReadArray(Ar.ReadString);
        BlendShapeChannelNames = Ar.ReadArray(Ar.ReadString);
        AnimatedMapNames = Ar.ReadArray(Ar.ReadString);
        MeshNames = Ar.ReadArray(Ar.ReadString);
        MeshBlendShapeChannelMapping = new RawSurjectiveMapping(Ar);
        JointHierarchy = Ar.ReadArray<ushort>();
        NeutralJointTranslations = new RawVector3Vector(Ar);
        NeutralJointRotations = new RawVector3Vector(Ar);
    }
}
