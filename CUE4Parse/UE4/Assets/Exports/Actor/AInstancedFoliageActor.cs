using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class AInstancedFoliageActor : AISMPartitionActor
{
    public Dictionary<FPackageIndex, FFoliageMeshInfo_Deprecated>? FoliageMeshes_Deprecated;
    public Dictionary<FPackageIndex, FFoliageMeshInfo_Deprecated2>? FoliageMeshes_Deprecated2;
    public Dictionary<FPackageIndex, FFoliageInfo>? FoliageInfos;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FFoliageCustomVersion.Get(Ar) < FFoliageCustomVersion.Type.CrossLevelBase)
        {
            FoliageMeshes_Deprecated = Ar.ReadMap(() => new FPackageIndex(Ar), () => new FFoliageMeshInfo_Deprecated(Ar));
        }
        else if (FFoliageCustomVersion.Get(Ar) < FFoliageCustomVersion.Type.FoliageActorSupport)
        {
            FoliageMeshes_Deprecated2 = Ar.ReadMap(() => new FPackageIndex(Ar), () => new FFoliageMeshInfo_Deprecated2(Ar));
        }
        else
        {
            FoliageInfos = Ar.ReadMap(() => new FPackageIndex(Ar), () => new FFoliageInfo(Ar));
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (FoliageMeshes_Deprecated != null)
        {
            writer.WritePropertyName("FoliageMeshes_Deprecated");
            serializer.Serialize(writer, FoliageMeshes_Deprecated);
        }
        else if (FoliageMeshes_Deprecated2 != null)
        {
            writer.WritePropertyName("FoliageMeshes_Deprecated2");
            serializer.Serialize(writer, FoliageMeshes_Deprecated2);
        }
        else if (FoliageInfos != null)
        {
            writer.WritePropertyName("FoliageInfos");
            serializer.Serialize(writer, FoliageInfos);
        }
    }
}

public struct FFoliageMeshInfo_Deprecated
{
    public FPackageIndex? Component;
    public FFoliageInstanceCluster_Deprecated[]? OldInstanceClusters;

    public FFoliageMeshInfo_Deprecated(FAssetArchive Ar)
    {
        if (FFoliageCustomVersion.Get(Ar) >= FFoliageCustomVersion.Type.FoliageUsingHierarchicalISMC)
        {
            Component = new FPackageIndex(Ar);
        }
        else
        {
            OldInstanceClusters = Ar.ReadArray(() => new FFoliageInstanceCluster_Deprecated(Ar));
        }
    }
}

public class FFoliageInstanceCluster_Deprecated(FAssetArchive Ar)
{
    public FBoxSphereBounds Bounds = new FBoxSphereBounds(Ar);
    public FPackageIndex[] ClusterComponent = Ar.ReadArray(() => new FPackageIndex(Ar));
}

public struct FFoliageMeshInfo_Deprecated2(FAssetArchive Ar)
{
    public FPackageIndex Component = new FPackageIndex(Ar);
}

public enum EFoliageImplType : byte
{
    Unknown = 0,
    StaticMesh = 1,
    Actor = 2,
    ISMActor = 3
}

public struct FFoliageInfo
{
    public EFoliageImplType Type;
    public FFoliageImpl? Implementation;

    public FFoliageInfo(FAssetArchive Ar)
    {
        Type = Ar.Read<EFoliageImplType>();
        Implementation = Type switch
        {
            EFoliageImplType.Unknown => null,
            EFoliageImplType.StaticMesh => new FFoliageStaticMesh(Ar),
            EFoliageImplType.Actor => new FFoliageActor(Ar),
            EFoliageImplType.ISMActor => null, // EDITORONLY_DATA
            _ => throw new NotImplementedException($"Foliage type {Type} not implemented"),
        };
    }
}

public abstract class FFoliageImpl;

public class FFoliageStaticMesh(FAssetArchive Ar) : FFoliageImpl
{
    public FPackageIndex Component = new FPackageIndex(Ar);
}

public class FFoliageActor : FFoliageImpl
{
    public FPackageIndex[] ActorInstances;
    public FPackageIndex[] ActorInstances_Deprecated;
    public FPackageIndex ActorClass;

    public FFoliageActor(FAssetArchive Ar)
    {
        if (FFoliageCustomVersion.Get(Ar) < FFoliageCustomVersion.Type.FoliageActorSupportNoWeakPtr)
        {
            ActorInstances_Deprecated = Ar.ReadArray(() => new FPackageIndex(Ar));
        }
        else
        {
            ActorInstances = Ar.ReadArray(() => new FPackageIndex(Ar));
        }
        ActorClass = new FPackageIndex(Ar);
    }
}

public class FFoliageISMActor : FFoliageImpl;
