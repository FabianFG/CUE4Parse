using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyLayerContainer : BaseHierarchy
{
    public uint[] ChildIDs { get; private set; }
    public List<AkPlayList.AkPlayListItem> Playlist { get; private set; }
    public byte IsContinuousValidation { get; private set; }

    public HierarchyLayerContainer(FArchive Ar) : base(Ar)
    {
        ChildIDs = new AkChildren(Ar).ChildIDs;
        Playlist = new AkPlayList(Ar).PlaylistItems;
        IsContinuousValidation = Ar.Read<byte>();
        Ar.Position += 2; // Padding?
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ChildIDs");
        serializer.Serialize(writer, ChildIDs);

        writer.WritePropertyName("PlayList");
        serializer.Serialize(writer, Playlist);

        writer.WritePropertyName("IsContinuousValidation");
        writer.WriteValue(IsContinuousValidation != 0);

        writer.WriteEndObject();
    }
}
