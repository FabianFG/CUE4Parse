using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly struct FKeyType(FName name, FName group) : IEquatable<FKeyType>
{
    public readonly FName Name = name;
    public readonly FName Group = group;

    public FKeyType(FAssetArchive Ar) : this(Ar.ReadFName(), Ar.ReadFName())
    {

    }

    public bool Equals(FKeyType other)
    {
        return Name.Equals(other.Name) && Group.Equals(other.Group);
    }

    public override bool Equals(object? obj)
    {
        return obj is FKeyType other && Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(Name, Group);

    public override string ToString() => $"{Group} -> {Name}";
}
