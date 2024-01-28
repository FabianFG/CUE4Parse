using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchyAudioBus : AbstractHierarchy
    {
        public HierarchyAudioBus(FArchive Ar) : base(Ar)
        {

        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
    }
}

