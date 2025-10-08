using System;
using System.Collections.Generic;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports.ControlRig.Rigs;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.ControlRig;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.ControlRig;

public class URigHierarchy : UObject
{
    public FRigBaseElement[] Elements;
    public Dictionary<FRigHierarchyKey, FRigHierarchyKey> PreviousHierarchyNameMap;
    public Dictionary<FRigHierarchyKey, FRigHierarchyKey> PreviousHierarchyParentMap;
    public Dictionary<FRigElementKey, FRigElementKey> PreviousNameMap = [];
    public Dictionary<FRigElementKey, FRigElementKey> PreviousParentMap = [];
    public Dictionary<FRigElementKey, FMetadataStorage> LoadedElementMetadata = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        var serializationSettings = new FRigHierarchySerializationSettings(Ar);
        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyCompactTransformSerialization)
        {
            serializationSettings.Load(Ar);
        }
        else if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.RigHierarchyCompressElements)
        {
            serializationSettings.bUseCompressedArchive = Ar.ReadBoolean();
        }

        serializationSettings.SerializationPhase = ESerializationPhase.StaticData;

        FArchive archiveForElements;
        if (serializationSettings.bUseCompressedArchive)
        {
            var uniqueNames = Ar.ReadArray(Ar.ReadFName);
            var uncompressedSize = Ar.Read<uint>();
            var bStoreCompressedBytes = Ar.ReadBoolean();
            var compressedBytes = Ar.ReadArray<byte>();

            var uncompressedBytes = new byte[uncompressedSize];
            if (bStoreCompressedBytes)
            {
                OodleHelper.Decompress(compressedBytes, 0, compressedBytes.Length, uncompressedBytes, 0, uncompressedBytes.Length);
            }

            var baseArchive = new FByteArchive("Archive for elements", bStoreCompressedBytes ? uncompressedBytes : compressedBytes, Ar.Versions);
            archiveForElements = new FRigHierarchyArchive(baseArchive, uniqueNames);
        }
        else
        {
            archiveForElements = Ar;
        }

        bool bAllocateStoragePerElement = FControlRigObjectVersion.Get(archiveForElements) < FControlRigObjectVersion.Type.RigHierarchyIndirectElementStorage;

        var elementCount = archiveForElements.Read<int>();
        Elements = new FRigBaseElement[elementCount];
        for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
        {
            var key = new FRigElementKey(archiveForElements);
            var element = key.Type switch
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
                element.Load(archiveForElements, this, serializationSettings);
            }

            Elements[elementIndex] = element;
        }

        if (!bAllocateStoragePerElement)
        {
            for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
            {
                Elements[elementIndex].Load(archiveForElements, this, serializationSettings);
            }
        }

        serializationSettings.SerializationPhase = ESerializationPhase.InterElementData;
        foreach (var element in Elements)
        {
            element.Load(archiveForElements, this, serializationSettings);
        }

        if (FControlRigObjectVersion.Get(archiveForElements) >= FControlRigObjectVersion.Type.RigHierarchyStoringPreviousNames)
        {
            if (FControlRigObjectVersion.Get(archiveForElements) >= FControlRigObjectVersion.Type.RigHierarchyPreviousNameAndParentMapUsingHierarchyKey)
            {
                PreviousHierarchyNameMap = archiveForElements.ReadMap(() => new FRigHierarchyKey(archiveForElements), () => new FRigHierarchyKey(archiveForElements));
                PreviousHierarchyParentMap = archiveForElements.ReadMap(() => new FRigHierarchyKey(archiveForElements), () => new FRigHierarchyKey(archiveForElements));
            }
            else
            {
                var previousNameMap = archiveForElements.ReadMap(() => new FRigElementKey(archiveForElements), () => new FRigElementKey(archiveForElements));
                var previousParentMap = archiveForElements.ReadMap(() => new FRigElementKey(archiveForElements), () => new FRigElementKey(archiveForElements));

                // TODO:
                // PreviousHierarchyNameMap.Reset();
                // for(const TPair<FRigElementKey, FRigElementKey>& Pair : PreviousNameMap)
                // {
                //     PreviousHierarchyNameMap.Add(Pair.Key, Pair.Value);
                // }
                // PreviousHierarchyParentMap.Reset();
                // for(const TPair<FRigElementKey, FRigElementKey>& Pair : PreviousParentMap)
                // {
                //     PreviousHierarchyParentMap.Add(Pair.Key, Pair.Value);
                // }
            }
        }

        if (FControlRigObjectVersion.Get(archiveForElements) >= FControlRigObjectVersion.Type.RigHierarchyStoresElementMetadata)
        {
            LoadedElementMetadata = archiveForElements.ReadMap(() => new FRigElementKey(archiveForElements), () => new FMetadataStorage(archiveForElements));
        }

        if (FControlRigObjectVersion.Get(archiveForElements) >= FControlRigObjectVersion.Type.RigHierarchyStoresComponents)
        {
            var numComponents = archiveForElements.Read<int>();

            if (numComponents > 0)
            {
                var scriptStructNames = Ar.ReadArray(Ar.ReadFString);
                throw new NotImplementedException();
            }
        }
    }
    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (Elements.Length > 0)
        {
            writer.WritePropertyName(nameof(Elements));
            serializer.Serialize(writer, Elements);
        }
    }
}
