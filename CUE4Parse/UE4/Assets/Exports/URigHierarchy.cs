using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports;

public class URigHierarchy : UObject
{
    public FRigBaseElement[] Elements;
    Dictionary<FRigElementKey, FRigElementKey> PreviousNameMap = [];
    Dictionary<FRigElementKey, FRigElementKey> PreviousParentMap = [];
    Dictionary<FRigElementKey, FMetadataStorage> LoadedElementMetadata = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        var ElementCount = Ar.Read<int>();
        Elements = new FRigBaseElement[ElementCount];
        for (var ElementIndex = 0; ElementIndex < ElementCount; ElementIndex++)
        {
            var key = new FRigElementKey(Ar);
            FRigBaseElement element = key.Type switch
            {
                ERigElementType.Bone => new FRigBoneElement(),
                ERigElementType.Null => new FRigNullElement(),
                ERigElementType.Control => new FRigControlElement(),
                ERigElementType.Curve => new FRigCurveElement(),
                ERigElementType.Reference => new FRigReferenceElement(),
                ERigElementType.RigidBody => new FRigRigidBodyElement(),
                ERigElementType.Connector => new FRigConnectorElement(),
                ERigElementType.Socket => new FRigSocketElement(),
                _ => new FRigBaseElement()
            };

            element.Load(Ar, this, ESerializationPhase.StaticData);
            Elements[ElementIndex] = element;
        }

        foreach (var element in Elements)
        {
            element.Load(Ar, this, ESerializationPhase.InterElementData);
        }

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyStoringPreviousNames)
        {
            var num = Ar.Read<int>();
            for (var i = 0; i < num; i++)
            {
                PreviousNameMap[new FRigElementKey(Ar)] = new FRigElementKey(Ar);
            }

            num = Ar.Read<int>();
            for (var i = 0; i < num; i++)
            {
                PreviousParentMap[new FRigElementKey(Ar)] = new FRigElementKey(Ar);
            }
        }

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyStoresElementMetadata)
        {
            var num = Ar.Read<int>();
            for (var i = 0; i < num; i++)
            {
                LoadedElementMetadata[new FRigElementKey(Ar)] = new FMetadataStorage(Ar);
            }
        }
    }
}
