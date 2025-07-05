using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.ControlRig;

public struct FRigHierarchySerializationSettings
{
    public FControlRigObjectVersion.Type ControlRigVersion;
    public bool bIsSerializingToPackage;
    public bool bUseCompressedArchive;
    public bool bStoreCompactTransforms;
    public bool bSerializeLocalTransform;
    public bool bSerializeGlobalTransform;
    public bool bSerializeInitialTransform;
    public bool bSerializeCurrentTransform;
    public ESerializationPhase SerializationPhase;

    public void Load(FArchive Ar)
    {
        ControlRigVersion = Ar.Read<FControlRigObjectVersion.Type>();
        bIsSerializingToPackage = Ar.ReadBoolean();
        bUseCompressedArchive = Ar.ReadBoolean();
        bStoreCompactTransforms = Ar.ReadBoolean();
        bSerializeLocalTransform = Ar.ReadBoolean();
        bSerializeGlobalTransform = Ar.ReadBoolean();
        bSerializeInitialTransform = Ar.ReadBoolean();
        bSerializeCurrentTransform = Ar.ReadBoolean();
        SerializationPhase = Ar.Read<ESerializationPhase>();
    }
}
