using System.Collections.Concurrent;
using System.Collections.Generic;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DuneAwakening.Assets.Objects;

public static class DAStructs
{
    private static ConcurrentDictionary<string, string?> _resolvedSuperStructs = new();

    public static IUStruct ParseDAStruct(FAssetArchive Ar, string? structName, UStruct? struc, ReadType? type)
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
                if (super is null) return superStructName;
                superStructName = mappingType.SuperType;
                mappingType = super;
            }
            return superStructName;
        });

        if (resolvedSuper == "StringEnumValue" && structName is not ("EItemCraftingRecipeId" or "ESchematicId"))
            return new FStringEnumValue(Ar);

        return structName switch
        {
            "BodyInstance" => new FBodyInstance(Ar),
            "GenericTeamId" => new FGenericTeamId(Ar),
            "UniqueID" or "Persistence_DEPRECATED_Id" or "PersistenceActorId"
                or "PersistenceItemId" or "PersistenceBuildingBlueprintId" => new FUniqueID(Ar),
            "BotAutoBorderCrossingConfig" => new FBotAutoBorderCrossingConfig(Ar),
            "StringEnumValue" => new FStringEnumValue(Ar),

            _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
        };
    }

    public static FPropertyTagData? ResolveSetPropertyInnerTypeData(FPropertyTagData tagData)
    {
        return tagData.Name switch
        {
            "AllowedItems" or "RequiredProductionTypes" or "m_SupportedProductionTypes" or "m_ActionsOnReceive"
                or "m_DesiredInputContexts" or "m_SupportedFuelTypes" or "m_MapMarkersToCount" or "m_ValidContactGroups"
                or "m_Placeables" or "m_Buildables" => new FPropertyTagData("StringEnumValue"),

            "ParameterInfoSet" => new FPropertyTagData("MaterialParameterInfo"),
            "CharacterStateTags" or "PrereqModuleTags_Or" or "PrereqModuleTags_And" => new FPropertyTagData("GameplayTag"),
            _ => null
        };
    }
}

public struct FBotAutoBorderCrossingConfig(FAssetArchive Ar) : IUStruct
{
    public bool m_bEnabled = Ar.ReadFlag();
    public float m_DetectBorderDistance = Ar.Read<float>();
    public float m_JumpOverBorderDistance = Ar.Read<float>();
}

public struct FGenericTeamId(FAssetArchive Ar) : IUStruct
{
    public byte TeamId = Ar.Read<byte>();
}
public struct FUniqueID(FAssetArchive Ar) : IUStruct
{
    public long UID = Ar.Read<long>();
}

public struct FStringEnumValue(FAssetArchive Ar) : IUStruct
{
    public FName Name = Ar.ReadFName();
}

public enum ECollisionResponse : byte
{
    Ignore = 0,
    Overlap = 1,
    Block = 2,
}

public struct FBodyInstance : IUStruct
{
    public FName CollisionEnabled;
    public Dictionary<FName, ECollisionResponse> ResponseArray;
    public ulong Flags;
    public FVector SomeVector;
    public FVector Scale;

    public FBodyInstance(FAssetArchive Ar)
    {
        CollisionEnabled = Ar.ReadFName();
        var count = Ar.Read<int>();
        var names = Ar.ReadArray(count, Ar.ReadFName);
        var values = Ar.ReadBytes(count);
        ResponseArray = new Dictionary<FName, ECollisionResponse>(count);
        for (var i = 0; i < count; i++)
        {
            ResponseArray[names[i]] = (ECollisionResponse) values[i];
        }
        Flags = Ar.Read<ulong>();

        SomeVector = Ar.Read<FVector>();
        Ar.Position += 96; // some floats/vectors and maybe some enum array at the end
        Scale = new FVector(Ar);
    }
}
