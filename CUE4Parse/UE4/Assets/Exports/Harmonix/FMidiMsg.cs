using System;
using System.IO;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Writers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Harmonix;

[JsonConverter(typeof(FMidiMsgConverter))]
public struct FMidiMsg : IUStruct
{
    public EType Type;

    public byte Status;
    public byte Data1;
    public byte Data2;

    public byte MicsPerQuarterNoteH;
    public ushort MicsPerQuarterNoteL;

    public byte Numerator;
    public byte Denominator;

    public byte TextType;
    public ushort TextIndex;

    public FMidiMsg(FAssetArchive Ar)
    {
        Type = Ar.Read<EType>();

        switch (Type)
        {
            case EType.Std:
                Status = Ar.Read<byte>();
                Data1 = Ar.Read<byte>();
                Data2 = Ar.Read<byte>();
                break;

            case EType.Tempo:
                MicsPerQuarterNoteH = Ar.Read<byte>();
                MicsPerQuarterNoteL = Ar.Read<ushort>();
                break;

            case EType.TimeSig:
                Numerator = Ar.Read<byte>();
                Denominator = Ar.Read<byte>();
                break;

            case EType.Text:
                TextType = Ar.Read<byte>();
                TextIndex = Ar.Read<ushort>();
                break;

            case EType.Runtime:
                // Nothing.
                break;

            default:
                throw new InvalidDataException($"Invalid FMidiMsg type: {Type}");
        }
    }
}

public class FMidiMsgConverter : JsonConverter<FMidiMsg>
{
    public override void WriteJson(JsonWriter writer, FMidiMsg value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Type");
        writer.WriteValue(value.Type.ToString());

        switch (value.Type)
        {
            case EType.Std:
                writer.WritePropertyName("Status");
                writer.WriteValue(value.Status);
                writer.WritePropertyName("Data1");
                writer.WriteValue(value.Data1);
                writer.WritePropertyName("Data2");
                writer.WriteValue(value.Data2);
                break;

            case EType.Tempo:
                writer.WritePropertyName("MicsPerQuarterNoteH");
                writer.WriteValue(value.MicsPerQuarterNoteH);
                writer.WritePropertyName("MicsPerQuarterNoteL");
                writer.WriteValue(value.MicsPerQuarterNoteL);
                break;

            case EType.TimeSig:
                writer.WritePropertyName("Numerator");
                writer.WriteValue(value.Numerator);
                writer.WritePropertyName("Denominator");
                writer.WriteValue(value.Denominator);
                break;

            case EType.Text:
                writer.WritePropertyName("TextType");
                writer.WriteValue(value.TextType);
                writer.WritePropertyName("TextIndex");
                writer.WriteValue(value.TextIndex);
                break;

            case EType.Runtime:
                // Nothing.
                break;
        }

        writer.WriteEndObject();
    }

    public override FMidiMsg ReadJson(JsonReader reader, Type objectType, FMidiMsg existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public enum EType : byte
{
    Std     = 1,
    Tempo   = 2,
    TimeSig = 4,
    Text    = 8,
    Runtime = 16
}