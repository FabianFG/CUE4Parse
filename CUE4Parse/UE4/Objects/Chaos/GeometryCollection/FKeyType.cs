using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly struct FKeyType : IUStruct, IEquatable<FKeyType>
{
    public readonly FName AttributeName;
    public readonly FName GroupName;

    public FKeyType(FArchive Ar)
    {
        AttributeName = Ar.ReadFName();
        GroupName = Ar.ReadFName();
    }

    public FKeyType(FName attributeName, FName groupName)
    {
        AttributeName = attributeName;
        GroupName = groupName;
    }

    public bool Equals(FKeyType other)
    {
        return AttributeName.Equals(other.AttributeName) && GroupName.Equals(other.GroupName);
    }

    public override bool Equals(object? obj)
    {
        return obj is FKeyType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AttributeName, GroupName);
    }

    public override string ToString() => $"{GroupName} -> {AttributeName}";
}
