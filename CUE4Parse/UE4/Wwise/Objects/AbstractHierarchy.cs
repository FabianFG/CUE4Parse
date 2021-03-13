using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public abstract class AbstractHierarchy
    {
        public uint Id { get; }

        protected AbstractHierarchy(FArchive Ar)
        {
            Id = Ar.Read<uint>();
        }
    }
}