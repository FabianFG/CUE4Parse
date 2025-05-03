using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class HierarchyMusicSwitchContainer : BaseHierarchyMusic
{
    public HierarchyMusicSwitchContainer(FArchive Ar) : base(Ar) { }

    //public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
}
