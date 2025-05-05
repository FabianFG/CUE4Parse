using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyLayerContainer : BaseHierarchy
{
    public uint[] ChildIds { get; private set; }
    public List<AkPlayList.AkPlayListItem> Playlist { get; private set; }
    public byte IsContinuousValidation { get; private set; }

    public HierarchyLayerContainer(FArchive Ar) : base(Ar)
    {
        ChildIds = new AkChildren(Ar).ChildIds;
        Playlist = new AkPlayList(Ar).PlaylistItems;
        IsContinuousValidation = Ar.Read<byte>();
        Ar.Position += 2; // Padding?
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ChildIds");
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName("PlayList");
        serializer.Serialize(writer, Playlist);

        writer.WritePropertyName("IsContinuousValidation");
        writer.WriteValue(IsContinuousValidation != 0);

        writer.WriteEndObject();
    }
}
