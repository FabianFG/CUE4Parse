using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Borderlands4.Assets.Objects;

public static class Borderlands4Structs
{
    public static IUStruct ParseBl4Struct(FAssetArchive Ar, string? structName, UStruct? struc, ReadType? type)
    {
        if (structName is null)
            return type == ReadType.ZERO ? new FStructFallback() :
                struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName);

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

            "SToken" => new FStructFallback(Ar, structName, FRawHeader.FullRead),
            "FactAddress" => new FStructFallback(Ar, structName, FRawHeader.FullRead),
            "GbxAttributeExpression" => new FStructFallback(Ar, structName, FRawHeader.FullRead),
            "GbxBlackboardEntryRef" => new FStructFallback(Ar, structName, FRawHeader.FullRead),
            "GbxNavGeometrySettings" => new FStructFallback(Ar, structName, FRawHeader.FullRead),
            "GbxActorStateMachineKey" => new FStructFallback(Ar, structName, FRawHeader.FullRead),
            "GbxActorStateMachineStateKey" => new FStructFallback(Ar, structName, new FRawHeader([(0,1)])),

            _ when type == ReadType.RAW => new FStructFallback(Ar, structName, FRawHeader.FullRead, ReadType.RAW),
            _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
        };
    }
}
