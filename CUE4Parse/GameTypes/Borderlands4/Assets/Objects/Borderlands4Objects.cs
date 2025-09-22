using CUE4Parse.GameTypes.Borderlands4.Assets.Objects.Properties;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.Borderlands4.Assets.Objects;

public class FGbxTrickRef : IUStruct
{
    public int Type;
    public FPackageIndex Trick;
    public FGameplayTag TrickTag;

    public FGbxTrickRef(FAssetArchive Ar)
    {
        Type = Ar.Read<int>();
        if (Type == 1)
        {
            TrickTag = new FGameplayTag(Ar);
        }
        else
        {
            Trick = new FPackageIndex(Ar);
        }
    }
}

public class FGbxInlineStruct: IUStruct
{
    public string Type;
    public FStructFallback? Struct;

    public FGbxInlineStruct(FAssetArchive Ar)
    {
        Type = Ar.ReadFString();
        if (string.IsNullOrEmpty(Type)) return;
        Struct = new FStructFallback(Ar, Type.SubstringAfterLast('.'));
    }
}

// using primary constructor trick to initialize Indices before base constructor
public class FGbxBrainTaskSettings(FAssetArchive Ar, string structName) : FStructFallback(Ar, structName)
{
    public int[] Indices = Ar.ReadArray<int>();
}

public class FGbxBlackboardEntryDefault : IUStruct
{
    public FName Name;
    public string Description;
    public bool EnabledExtensions;
    public FName Extension;
    public FGbxParam Value;

    public FGbxBlackboardEntryDefault(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        Ar.Position += 4;
        Description = Ar.ReadFString();
        EnabledExtensions = Ar.ReadBoolean();
        if (EnabledExtensions)
        {
            Extension = Ar.ReadFName();
        }
        Value = new FGbxParam(Ar);
    }
}

public class FGbxGraphParam(FAssetArchive Ar) : FGbxParam(Ar)
{
    public FName Name = Ar.ReadFName();
    public string Description = Ar.ReadFString();
    public bool bIsPrivate = Ar.ReadBoolean();
}

public enum EGbxType : byte
{
    None = 0,
    Bool = 1,
    Int = 2,
    Float = 3,
    Vector = 4,
    Rotator = 5,
    Object = 6,
    Actor = 7,
    String = 8,
    NavSpot = 9,
    Attribute = 10,
    NumericRange = 11,
    TargetInfo = 12,
    GraphNodeOutput = 13,
    SceneComponent = 14,
    TrajectoryOptions = 15,
    Waypoint = 16,
    GraphParam = 17,
    Name = 18,
    WorldStateRegistryActor = 19,
    Blackboard = 20,
    Double = 21,
    MissionAliasRef = 22,
    DataTable = 23,
    LinearColor = 24,
    HitResult = 25,
    ForceSelection = 26,
    Text = 27,
    Asset = 28,
    GbxDef = 29,
    WeightedAttributeInit = 30,
    FactAddress = 31,
    DialogEnumValue = 32,
    AttributeEvaluator = 33,
    GameplayTag = 34,
}

public class FGbxParam : IUStruct
{
    public EGbxType Type;
    public object? Value;
    public EGbxType EndByte;

    public FGbxParam(FAssetArchive Ar)
    {
        Type = Ar.Read<EGbxType>();
        Value = Type switch
        {
            EGbxType.None => null,
            EGbxType.Bool => Ar.ReadFlag(),
            EGbxType.Int => Ar.Read<int>(),
            EGbxType.Float => Ar.Read<float>(),
            EGbxType.Vector => new FVector(Ar),
            EGbxType.Rotator => new FRotator(Ar),
            EGbxType.Object or EGbxType.Actor => new FPackageIndex(Ar),
            EGbxType.String => Ar.ReadFString(),
            // EGbxType.NavSpot => GbxNavSpot
            EGbxType.Attribute => new FGameDataHandle(Ar),
            EGbxType.NumericRange => new FStructFallback(Ar, "NumericRange"),
            // EGbxType.TargetInfo
            EGbxType.GraphNodeOutput => Ar.ReadArray<int>(), // maybe
            // EGbxType.SceneComponent
            EGbxType.TrajectoryOptions => new FStructFallback(Ar, "TrajectoryOptions"),
            // EGbxType.Waypoint
            EGbxType.GraphParam => new FStructFallback(Ar, "GbxAttributeRef"),
            EGbxType.Name or EGbxType.Blackboard => Ar.ReadFName(),
            EGbxType.WorldStateRegistryActor => new FStructFallback(Ar, "FactsSystemActorReference"), // ??
            EGbxType.Double => Ar.Read<double>(),
            EGbxType.MissionAliasRef => new FStructFallback(Ar, "MissionAliasRef"),
            EGbxType.DataTable => new FStructFallback(Ar, "DataTableValueHandle"),
            EGbxType.LinearColor => Ar.Read<FLinearColor>(),
            // EGbxType.HitResult
            EGbxType.ForceSelection => new FStructFallback(Ar, "ForceSelection"),
            EGbxType.Text => new FText(Ar),
            EGbxType.Asset => new FSoftObjectPath(Ar),
            EGbxType.GbxDef => new FGbxDefPtr(Ar),
            EGbxType.WeightedAttributeInit => new FStructFallback(Ar, "GbxWeightedAttributeInit"), // only 1 entry
            // EGbxType.FactAddress
            // EGbxType.DialogEnumValue
            EGbxType.AttributeEvaluator => new FStructFallback(Ar, "GbxAttributeEvaluator"),
            EGbxType.GameplayTag => new FGameplayTag(Ar),
            _ => throw new ParserException($"FGbxParam type {Type} with value {Value} isn't suppoerted at pos {Ar.Position}." ),
        };
        EndByte = Ar.Read<EGbxType>(); // usually equal to the Type, but sometimes it's different
    }
}

public class FHavokNavMesh : IUStruct
{
    public byte[] HavokNavMeshData;

    public FHavokNavMesh(FAssetArchive Ar)
    {
        //HavokNavMeshData = Ar.ReadArray<byte>());
        Ar.SkipFixedArray(1);
        Ar.Position += 28; // metadata for chunks
    }
}

public class FGbxSkillCompValue : IUStruct
{
    public FSoftObjectPath? ObjectParam;
    public FGbxParam? GbxParam;

    public FGbxSkillCompValue(FAssetArchive Ar)
    {
        if (Ar.ReadBoolean())
        {
            ObjectParam = new FSoftObjectPath(Ar);
        }
        else
        {
            GbxParam = new FGbxParam(Ar);
        }
    }
}

public class FDialogParameterValue : IUStruct
{
    public int Type;
    public object? Value;

    public FDialogParameterValue(FAssetArchive Ar)
    {
        Type = Ar.Read<int>(); // EDialogParameterType
        Value = Type switch
        {
            3 => Ar.Read<int>(),
            5 => Ar.ReadFName(),
            _ => null,
        };
    }
}

public class FHavokFlightNav : IUStruct
{
    public FHavokFlightNav(FAssetArchive Ar)
    {
        Ar.SkipFixedArray(1);
        Ar.Position += 16;
    }
}

public class FFactExpression(FAssetArchive Ar) : IUStruct
{
    public string Expression = Ar.ReadFString();
}

public class FDamageSourceContainer(FAssetArchive Ar) : IUStruct
{
    public FGameplayTagContainer Container = new(Ar.ReadArray(Ar.Read<byte>(), () => new FGameplayTag(Ar)));
}

public class FSName : IUStruct
{
    public int TokenCount;
    public int SummaryHash;
    public FStructFallback InlineTokens;
    public FStructFallback[] ExternalTokens;

    public FSName(FAssetArchive Ar)
    {
        TokenCount = Ar.Read<int>();
        if (TokenCount > 0)
        {
            SummaryHash = Ar.Read<int>();
            ExternalTokens = Ar.ReadArray(TokenCount, () => new FStructFallback(Ar, "SToken", FRawHeader.FullRead));
            InlineTokens = ExternalTokens[0];
        }
    }
}
