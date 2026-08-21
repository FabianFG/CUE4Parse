using CUE4Parse.GameTypes.Borderlands4.Assets.Objects.Properties;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Borderlands4.Assets.Objects;

public static class Borderlands4Structs
{
    public static IUStruct ParseBl4Struct(FAssetArchive Ar, string? structName, string? fullTypeIdentifier, UStruct? struc, ReadType? type)
    {
        var fallbackIdentifier = fullTypeIdentifier ?? structName;
        if (structName is null)
            return type == ReadType.ZERO ? new FStructFallback() :
                struc != null ? new FStructFallback(Ar, struc, fullTypeIdentifier) : new FStructFallback(Ar, fallbackIdentifier);

        return structName switch
        {
            "SName" => new FSName(Ar),
            "GbxParam" => new FGbxParam(Ar),
            "GbxTrickRef" => new FGbxTrickRef(Ar),
            "HavokNavMesh" => new FHavokNavMesh(Ar),
            "GbxGraphParam" => new FGbxGraphParam(Ar),
            "HavokFlightNav" => new FHavokFlightNav(Ar),
            "FactExpression" => new FFactExpression(Ar),
            "GbxInlineStruct" => new FGbxInlineStruct(Ar),
            "GbxSkillCompValue" => new FGbxSkillCompValue(Ar),
            "DialogParameterValue" => new FDialogParameterValue(Ar),
            "DamageSourceContainer" => new FDamageSourceContainer(Ar),
            "GbxBlackboardEntryDefault" => new FGbxBlackboardEntryDefault(Ar),
            "GbxBodyNodeSettings_SceneComponentInstanceData" => new FGbxInlineStruct(Ar),

            "NexusBitSet" or "DamageTags" or "GbxVenueTags"
                or "InventoryTags" => type == ReadType.ZERO ? new FGameplayTagContainer() : new FGameplayTagContainer(Ar),

            "SToken" => new FStructFallback(Ar, fallbackIdentifier, FRawHeader.FullRead),
            "FactAddress" => new FStructFallback(Ar, fallbackIdentifier, FRawHeader.FullRead),
            "GbxAttributeExpression" => new FStructFallback(Ar, fallbackIdentifier, FRawHeader.FullRead),
            "GbxBlackboardEntryRef" => new FStructFallback(Ar, fallbackIdentifier, FRawHeader.FullRead),
            "GbxNavGeometrySettings" => new FStructFallback(Ar, fallbackIdentifier, FRawHeader.FullRead),
            "GbxActorStateMachineKey" => new FStructFallback(Ar, fallbackIdentifier, FRawHeader.FullRead),
            "GbxActorStateMachineStateKey" => new FStructFallback(Ar, fallbackIdentifier, new FRawHeader([(0,1)])),

            _ when type == ReadType.RAW => new FStructFallback(Ar, fallbackIdentifier, FRawHeader.FullRead, ReadType.RAW),
            _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc, fullTypeIdentifier) : new FStructFallback(Ar, fallbackIdentifier)
        };
    }
}

[StructFallback]
public class FGbxAudioBodyAction_PostEvent
{
    public FGbxAudioEvent ActivationSound;
    public FGbxAudioEvent DeactivationSound;

    public FGbxAudioBodyAction_PostEvent(FStructFallback fallback)
    {
        ActivationSound = fallback.GetOrDefault<FGbxAudioEvent>(nameof(ActivationSound));
        DeactivationSound = fallback.GetOrDefault<FGbxAudioEvent>(nameof(DeactivationSound));
    }
}

[StructFallback]
public class FGbxAudioEvent
{
    public bool bUseSoundTag;
    public FGameplayTag SoundTag;
    public FGbxDefPtr WwiseEvent;

    public FGbxAudioEvent(FStructFallback fallback)
    {
        bUseSoundTag = fallback.GetOrDefault<bool>(nameof(bUseSoundTag));
        SoundTag = fallback.GetOrDefault<FGameplayTag>(nameof(SoundTag));
        WwiseEvent = fallback.GetOrDefault<FGbxDefPtr>(nameof(WwiseEvent));
    }
}

[StructFallback]
public class FGbxAudioBodyAction_ManagedLoop
{
    public FGbxAudioEvent LoopStartEvent;
    public FGbxAudioEvent LoopStopEvent;

    public FGbxAudioBodyAction_ManagedLoop(FStructFallback fallback)
    {
        LoopStartEvent = fallback.GetOrDefault<FGbxAudioEvent>(nameof(LoopStartEvent));
        LoopStopEvent = fallback.GetOrDefault<FGbxAudioEvent>(nameof(LoopStopEvent));
    }
}

[StructFallback]
public class FGbxAudioNodeAspectSettings_PostEvent
{
    public FGbxAudioEvent AudioEvent;
    public bool bStopEventsOnGraphEnd;
    public FGbxAudioEvent StopEvent;
    public FName Name;

    public FGbxAudioNodeAspectSettings_PostEvent(FStructFallback fallback)
    {
        AudioEvent = fallback.GetOrDefault<FGbxAudioEvent>(nameof(AudioEvent));
        bStopEventsOnGraphEnd = fallback.GetOrDefault<bool>(nameof(bStopEventsOnGraphEnd));
        StopEvent = fallback.GetOrDefault<FGbxAudioEvent>(nameof(StopEvent));
        Name = fallback.GetOrDefault<FName>(nameof(Name));
    }
}

[StructFallback]
public class GbxFaceFXAnimData : IUStruct
{
    public FaceFXAnimId ID;
    public FName WwiseCombinedEventName;
    public int AnimBufferIndex;
    public int AnimBufferLength;

    public GbxFaceFXAnimData(FStructFallback fallback)
    {
        ID = fallback.GetOrDefault<FaceFXAnimId>(nameof(ID));
        WwiseCombinedEventName = fallback.GetOrDefault<FName>(nameof(WwiseCombinedEventName));
        AnimBufferIndex = fallback.GetOrDefault<int>(nameof(AnimBufferIndex));
        AnimBufferLength = fallback.GetOrDefault<int>(nameof(AnimBufferLength));
    }
}

[StructFallback]
public class FaceFXAnimId : IUStruct
{
    public FName Group;
    public FName Name;

    public FaceFXAnimId(FStructFallback fallback)
    {
        Group = fallback.GetOrDefault<FName>(nameof(Group));
        Name = fallback.GetOrDefault<FName>(nameof(Name));
    }
}
