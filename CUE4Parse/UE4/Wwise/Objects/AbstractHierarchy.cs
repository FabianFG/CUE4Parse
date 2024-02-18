using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public abstract class AbstractHierarchy
    {
        public uint Id { get; }

        protected AbstractHierarchy(FArchive Ar)
        {
            Id = Ar.Read<uint>();
        }

        public abstract void WriteJson(JsonWriter writer, JsonSerializer serializer);
    }
}
