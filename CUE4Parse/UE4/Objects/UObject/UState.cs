using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.UObject;

[Flags]
public enum EStateFlags : uint
{
    Editable    = 1 << 0,
    Auto        = 1 << 1,
    Simulated   = 1 << 2,
}

public class UState : UStruct
{
    public ulong ProbeMask;
    public ulong IgnoreMask;
    public short LabelTableOffset;
    [JsonConverter(typeof(StringEnumConverter))]
    public EStateFlags StateFlags;
    public Dictionary<FName, FPackageIndex /*UFunction*/> FuncMap;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.REDUCED_PROBEMASK_REMOVED_IGNOREMASK)
        {
            ProbeMask = Ar.Read<ulong>();
            IgnoreMask = Ar.Read<ulong>();
        }
        else
        {
            ProbeMask = Ar.Read<uint>();
        }

        LabelTableOffset = Ar.Read<short>();
        StateFlags = Ar.Read<EStateFlags>();
        if (Ar.Ver > EUnrealEngineObjectUE3Version.MovedFriendlyNameToUFunction)
        {
            FuncMap = Ar.ReadMap(Ar.ReadFName, () => new FPackageIndex(Ar));
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ProbeMask");
        writer.WriteValue(ProbeMask);

        writer.WritePropertyName("IgnoreMask");
        writer.WriteValue(IgnoreMask);

        writer.WritePropertyName("LabelTableOffset");
        writer.WriteValue(LabelTableOffset);

        writer.WritePropertyName("StateFlags");
        writer.WriteValue(StateFlags);

        if (FuncMap is { Count: > 0 })
        {
            writer.WritePropertyName("FuncMap");
            serializer.Serialize(writer, FuncMap);
        }
    }
}