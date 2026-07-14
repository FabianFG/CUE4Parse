using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject;

public class UEnum : UField
{
    /** List of pairs of all enum names and values. */
    public (FName, long)[] Names;

    /** How the enum was originally defined. */
    public ECppForm CppForm;

    /** The underlying enum type. */
    public EUnderlyingType UnderlyingType = EUnderlyingType.int64;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Ver < EUnrealEngineObjectUE4Version.TIGHTLY_PACKED_ENUMS)
        {
            var tempNames = Ar.ReadArray(Ar.ReadFName);
            Names = new (FName, long)[tempNames.Length];
            for (var value = 0; value < tempNames.Length; value++)
            {
                Names[value] = (tempNames[value], value);
            }
        }
        else if (FCoreObjectVersion.Get(Ar) < FCoreObjectVersion.Type.EnumProperties)
        {
            if (Ar.Game == GAME_StateOfDecay2) Ar.Position += 4;
            var oldNames = Ar.ReadArray(() => (Ar.ReadFName(), Ar.Read<byte>()));
            Names = new (FName, long)[oldNames.Length];
            for (var value = 0; value < oldNames.Length; value++)
            {
                Names[value] = oldNames[value];
            }
        }
        else
        {
            Names = Ar.ReadArray(() => (Ar.ReadFName(), Ar.Read<long>()));
        }

        if (Ar.Ver < EUnrealEngineObjectUE4Version.ENUM_CLASS_SUPPORT)
        {
            var bIsNamespace = Ar.Game >= GAME_UE4_0 && Ar.ReadBoolean();
            CppForm = bIsNamespace ? ECppForm.Namespaced : ECppForm.Regular;
        }
        else
        {
            CppForm = Ar.Read<ECppForm>();
        }

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.EnumUnderlyingType)
        {
            UnderlyingType = Ar.Read<EUnderlyingType>();
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Names));
        writer.WriteStartObject();
        {
            foreach (var (name, enumValue) in Names)
            {
                writer.WritePropertyName(name.Text);
                writer.WriteValue(enumValue);
            }
        }
        writer.WriteEndObject();

        writer.WritePropertyName(nameof(CppForm));
        writer.WriteValue(CppForm.ToString());
    }

    public enum ECppForm : byte
    {
        Regular,
        Namespaced,
        EnumClass
    }

    public enum EUnderlyingType : byte
    {
        int8,
        int16,
        int32,
        int64,
        uint8,
        uint16,
        uint32,
        uint64,
    };

}
