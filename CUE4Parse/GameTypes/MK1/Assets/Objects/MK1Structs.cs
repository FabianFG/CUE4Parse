using System.Collections.Concurrent;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse.GameTypes.MK1.Assets.Objects;

public static class MK1Structs
{
    private static ConcurrentDictionary<string, string?> _resolvedSuperStructs = new();

    public static IUStruct ParseMK1Struct(FAssetArchive Ar, string? structName, UStruct? struc, ReadType? type)
    {
        if (structName is null)
            return type == ReadType.ZERO ? new FStructFallback() :
                struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName);

        var mappings = Ar.Owner?.Mappings?.Types;
        var resolvedSuper = _resolvedSuperStructs.GetOrAdd(structName, s =>
        {
            var mappingType = mappings?.GetValueOrDefault(s);
            string? superStructName = mappingType?.SuperType;
            while (mappingType is not null)
            {
                var super = mappingType.Super.Value;
                if (super is null)
                    return superStructName;
                superStructName = mappingType.SuperType;
                mappingType = super;
            }
            return superStructName;
        });

        return resolvedSuper switch
        {
            "GameParameterBase" => new FGameParameterBase(Ar),
            "MKInventoryItemPtrBase" => new MKInventoryItemPtrBase(Ar),
            "StructPtrBase" => new FStructPtrBase(Ar),
            _ => structName switch
            {
                "CompressedFloatTrackData" => new FCompressedFloatTrackData(Ar),
                "MovieSceneFieldEntry_ChildTemplate" or "MovieSceneEvaluationGroupLUTIndex" or "MovieSceneFieldEntry_EvaluationTrack"
                    or "MovieSceneOrderedEvaluationKey" or "MovieSceneEvaluationFieldTrackPtr" or "PathNodeInfo" or "TrackSetInfo"
                    or "TrackConfigInfo" or "TimelineKeySampleData" or "MKInventoryItemSlots" or "PoseData" => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),
                "CompiledTimelinePredicate" => new FCompiledTimelinePredicate(Ar),
                "TimelinePredicateState" => new FTimelinePredicateState(Ar),
                "MKLootDropItemPicker" => new FMKLootDropItemPicker(Ar),
                "Transform" when type is ReadType.RAW => Ar.Read<FTransform>(),
                "BuffPropertyModificationPtr" => new FStructFallback(),

                _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
            }
        };
    }
}

public class FCompressedFloatTrackData : IUStruct
{
    public AnimationCompressionFormat Format;
    public FFrameNumber[] Keys;
    public byte[] DataBuffer;

    public FCompressedFloatTrackData(FAssetArchive Ar)
    {
        Format = Ar.Read<AnimationCompressionFormat>();
        Keys = Ar.ReadBulkArray<FFrameNumber>();
        DataBuffer = Ar.ReadBulkArray<byte>();
        //using var reader = new FByteArchive("CompressedFloatTrackDataBuffer", DataBuffer, Ar.Versions);
    }
}

public class FTimelinePredicateState(FAssetArchive Ar) : IUStruct
{
    public ulong[] Unknown = Ar.ReadArray<ulong>();
}

public class FCompiledTimelinePredicate(FAssetArchive Ar) : IUStruct
{
    public byte[] Unknown = Ar.ReadArray<byte>();
    public object[] Parameters = Ar.ReadArray(() => ReadParameterValue(Ar));

    public static object ReadParameterValue(FAssetArchive Ar)
    {
        var type = Ar.ReadFName();
        object? res = type.Text switch
        {
            "bool" => Ar.ReadBoolean(),
            "float" => Ar.Read<float>(),
            "int32" => Ar.Read<int>(),
            "object" or "UObject*" => new FPackageIndex(Ar),
            "FVector" => Ar.Read<FVector>(),
            "FName" => Ar.ReadFName(),
            _ => null
        };
        if (res is null)
        {
            Log.Warning("Unknown MK1 parameter type {0}", type);
        }
        return res;
    }
}

public class FGameParameterBase(FAssetArchive Ar) : IUStruct
{
    public object Value = FCompiledTimelinePredicate.ReadParameterValue(Ar);
    public byte[] data = Ar.ReadArray<byte>(6);
    public FName Type = Ar.ReadFName();
}

public class UMKDialogueTable : UDataTable
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        var numRows = Ar.Read<int>();
        RowMap = new Dictionary<FName, FStructFallback>(numRows);
        for (var i = 0; i < numRows; i++)
        {
            var rowName = Ar.ReadFName();
            List<FPropertyTag> properties = [];
            DeserializePropertiesTagged(properties, Ar, true);
            RowMap[rowName] = new FStructFallback(properties);
        }
    }
}

public class MKInventoryItemPtrBase : IUStruct
{
    public FStructFallback FallbackStruct;
    public FScriptStruct? NonConstStruct;

    public MKInventoryItemPtrBase(FAssetArchive Ar)
    {
        FallbackStruct = new FStructFallback(Ar, "MKInventoryItemPtrBase");
        NonConstStruct = FScriptStruct.ReadInstancedStructWithoutSerialSize(Ar);
    }
}

public class FMKLootDropItemPicker : IUStruct
{
    public readonly FStructFallback FallbackStruct;
    public readonly FScriptStruct? NonConstStruct;
    public FMKLootDropItemPicker(FAssetArchive Ar)
    {
        FallbackStruct = new FStructFallback(Ar, "MKLootDropItemPicker");
        var mLootStruct = FallbackStruct.GetOrDefault<FPackageIndex>("mLootStruct");
        NonConstStruct = FScriptStruct.ReadInstancedStructWithoutSerialSize(Ar, mLootStruct);
    }
}
public class FStructPtrBase(FAssetArchive Ar) : IUStruct
{
    public FStructFallback FallbackStruct = new(Ar, "StructPtrBase");
    public FScriptStruct? NonConstStruct = FScriptStruct.ReadInstancedStructWithoutSerialSize(Ar);
}
