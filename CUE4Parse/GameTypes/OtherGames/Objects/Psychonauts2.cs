using System.Runtime.InteropServices;
using System.Text;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class FLinecode(FAssetArchive Ar) : IUStruct
{
    public string Value = Encoding.UTF8.GetString(Ar.ReadBytes(13));
}

public enum EAttribute : byte
{
    Invalid = 0,
    Enum = 1,
    Integer = 2,
    Float = 3,
    Boolean = 4,
    String = 5,
    Text = 6,
    Name = 7,
    Object = 8,
    Class = 9,
    Vector = 10,
    Quat = 11,
    Color = 12,
    NUM = 13
}

public class FP2Attribute : IUStruct
{
    public EAttribute Type;
    public object Value;

    public FP2Attribute(FAssetArchive Ar)
    {
        Type = Ar.Read<EAttribute>();
        Value = Type switch
        {
            EAttribute.Invalid => null!,
            EAttribute.Enum => Ar.ReadFName(),
            EAttribute.Integer => Ar.Read<int>(),
            EAttribute.Float => Ar.Read<float>(),
            EAttribute.Boolean => Ar.ReadBoolean(),
            EAttribute.String => Ar.ReadFString(),
            EAttribute.Text => new FText(Ar),
            EAttribute.Name => Ar.ReadFName(),
            EAttribute.Object => new FPackageIndex(Ar),
            EAttribute.Class => new FPackageIndex(Ar),
            EAttribute.Vector => Ar.Read<FVector>(),
            EAttribute.Quat => Ar.Read<FQuat>(),
            EAttribute.Color => Ar.Read<FLinearColor>(),
            _ => throw new ParserException($"Unsupported EAttribute type : {Type}"),
        };
    }
}

public class UStoryData : UObject
{
    public FP2StoryDataStruct[] Data;
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Data = Ar.ReadArray<FP2StoryDataStruct>();
    }

    [StructLayout(LayoutKind.Sequential, Pack =1)]
    public struct FP2StoryDataStruct
    {
        public int TextsIndex;
        public int DialogueIndex;
        public short CharactersIndex;
        public short CharacterVariantsIndex;
        public short AudioTemplatesIndex;
        public short LipSyncAnimsIndex;
        public byte Type;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(Data));
        serializer.Serialize(writer, Data);
    }
}
