using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkSwitchPackage
{
    public uint SwitchID { get; private set; }
    public List<uint> NodeIDs { get; private set; }

    public AkSwitchPackage(FArchive Ar)
    {
        SwitchID = Ar.Read<uint>();
        var numItems = Ar.Read<uint>();
        NodeIDs = [];
        for (var i = 0; i < numItems; i++)
        {
            NodeIDs.Add(Ar.Read<uint>());
        }
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("SwitchID");
        writer.WriteValue(SwitchID);

        writer.WritePropertyName("NodeIDs");
        serializer.Serialize(writer, NodeIDs);

        writer.WriteEndObject();
    }
}
