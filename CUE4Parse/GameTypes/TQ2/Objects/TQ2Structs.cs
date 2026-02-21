using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse.GameTypes.TQ2.Objects;

public static class TQ2Structs
{
    public static IUStruct ParseTQ2Struct(FAssetArchive Ar, string? structName, UStruct? struc, ReadType? type)
    {
        if (structName is null) return type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName);
        return structName switch
        {
            "GrimItemDescriptionPtr" or "GrimAbilityProjectileParametersPtr" or "SoftJsonDataAssetPtr" or
                "JsonDataAssetPath" or "JsonDataAssetPtr" => new FSoftObjectPath(Ar),
            "GrimDialogueVariableValue" => new FGrimDialogueVariableValue(Ar),
            _ when structName.StartsWith("GrimDialogue") && structName.EndsWith("Ref") => new FArticyId(Ar),
            _ when structName.StartsWith("TQ2") && structName.EndsWith("Ptr") => new FSoftObjectPath(Ar),
            _ when structName.StartsWith("Grim") && structName.EndsWith("SoftPtr") => new FSoftObjectPath(Ar),
            _ when structName.StartsWith("Grim") && structName.EndsWith("Ptr") => new FGrimInstancedObjectPtr(Ar),
            _ => type == ReadType.ZERO ? new FStructFallback() : struc != null ? new FStructFallback(Ar, struc) : new FStructFallback(Ar, structName)
        };
    }
}

public enum EGrimDialogueVariableType : byte
{
    Bool,
    Int,
    Float,
    String,
    NumOf,
}

public class FGrimDialogueVariableValue : IUStruct
{
    public EGrimDialogueVariableType Type;
    public dynamic Value;

    public FGrimDialogueVariableValue(FAssetArchive Ar)
    {
        Type = Ar.Read<EGrimDialogueVariableType>();
        Value = Type switch
        {
            EGrimDialogueVariableType.Bool => Ar.ReadBoolean(),
            EGrimDialogueVariableType.Int => (Ar.Read<int>(), Ar.Read<int>()),
            _ => throw new ParserException($"Unsupported GrimDialogueVariableType: {Type}"),
        };
    }
}

public struct FArticyId(FAssetArchive Ar) : IUStruct
{
    public ulong ID = Ar.Read<ulong>();
}

public class FGrimInstancedObjectPtr : IUStruct
{
    public int Type;
    public int Index;
    public FPackageIndex StructType;
    public FStructFallback? NonConstStruct;

    public FGrimInstancedObjectPtr(FAssetArchive Ar)
    {
        var bValid = Ar.ReadBoolean();
        StructType = new FPackageIndex(Ar);
        if (StructType.IsNull || !bValid) return;

        Type = Ar.Read<int>();
        Index = Ar.Read<int>();

        if (StructType.TryLoad<UStruct>(out var struc))
        {
            NonConstStruct = new FStructFallback(Ar, struc);
        }
        else if (StructType.ResolvedObject is { } obj)
        {
            NonConstStruct = new FStructFallback(Ar, obj.Name.ToString());
        }
        else
        {
            Log.Warning("Failed to read FGrimInstancedObjectPtr of type {0}, skipping it", StructType.ResolvedObject?.GetFullName());
        }

        Ar.Position += 4; // zero
    }
}
