using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkGameSync
{
    public readonly uint GroupId;
    public byte GroupType { get; private set; }

    public AkGameSync(FArchive Ar)
    {
        GroupId = Ar.Read<uint>();
    }

    public void SetGroupType(FArchive Ar)
    {
        GroupType = Ar.Read<byte>();
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("GroupId");
        writer.WriteValue(GroupId);

        writer.WritePropertyName("GroupType");
        writer.WriteValue(GroupType);

        writer.WriteEndObject();
    }
}
