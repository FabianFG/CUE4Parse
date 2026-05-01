using System;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.V2.Dto;

public abstract class ObjectDto(UObject owner, string? name = null) : IDisposable, IEquatable<ObjectDto>
{
    public readonly string Name = name ?? owner.Name;
    public readonly string Path = owner.Owner?.Provider?.FixPath(owner.Owner?.Name ?? owner.GetPathName()) ?? "N/A"; // full fixed .uasset path

    public abstract void Dispose();

    public override string ToString() => Name;

    public bool Equals(ObjectDto? other) => other is not null && (ReferenceEquals(this, other) || (Name == other.Name && Path == other.Path));
    public override bool Equals(object? obj) => obj?.GetType() == GetType() && Equals((ObjectDto) obj);
    public override int GetHashCode() => HashCode.Combine(Name, Path);

    public static bool operator ==(ObjectDto? left, ObjectDto? right) => Equals(left, right);
    public static bool operator !=(ObjectDto? left, ObjectDto? right) => !Equals(left, right);
}
