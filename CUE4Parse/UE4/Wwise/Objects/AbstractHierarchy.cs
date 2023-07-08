using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public abstract class AbstractHierarchy(FArchive Ar)
{
    public uint Id { get; } = Ar.Read<uint>();
}
