using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports;

public class URigHierarchy : UObject
{
    public FRigBaseElement[] Elements;
    public Dictionary<FRigElementKey, FRigElementKey> PreviousNameMap = [];
    public Dictionary<FRigElementKey, FRigElementKey> PreviousParentMap = [];
    public Dictionary<FRigElementKey, FMetadataStorage> LoadedElementMetadata = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        bool bAllocateStoragePerElement = FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.RigHierarchyIndirectElementStorage;
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

            if (bAllocateStoragePerElement)
            {
                element.Load(Ar, this, ESerializationPhase.StaticData);
            }

            Elements[ElementIndex] = element;
        }

        if (!bAllocateStoragePerElement)
        {
            for (var ElementIndex = 0; ElementIndex < ElementCount; ElementIndex++)
            {
                Elements[ElementIndex].Load(Ar, this, ESerializationPhase.StaticData);
            }
        }

        foreach (var element in Elements)
        {
            element.Load(Ar, this, ESerializationPhase.InterElementData);
        }

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyStoringPreviousNames)
        {
            PreviousNameMap = Ar.ReadMap(() => new FRigElementKey(Ar), () => new FRigElementKey(Ar));
            PreviousParentMap = Ar.ReadMap(() => new FRigElementKey(Ar), () => new FRigElementKey(Ar));
        }

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyStoresElementMetadata)
        {
            LoadedElementMetadata = Ar.ReadMap(() => new FRigElementKey(Ar), () => new FMetadataStorage(Ar));
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (Elements?.Length > 0)
        {
            writer.WritePropertyName(nameof(Elements));
            serializer.Serialize(writer, Elements);
        }
    }
}
