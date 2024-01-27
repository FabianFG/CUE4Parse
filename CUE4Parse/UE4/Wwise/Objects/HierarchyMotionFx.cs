using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchyMotionFx : AbstractHierarchy
    {
        public HierarchyMotionFx(FArchive Ar, long hierarchyEndPosition) : base(Ar)
        {
            Ar.Position = hierarchyEndPosition;
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
    }
}
