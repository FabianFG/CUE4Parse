using System;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.V2.Dto;

public abstract class ObjectDto : IDisposable
{
    public readonly string Name;
    public readonly string[] OuterNames;
    public readonly string Path;

    protected ObjectDto(UObject owner, string? name = null)
    {
        var path = owner.GetPathName();
        Name = name ?? owner.Name;
        OuterNames = path.Split(':') is { Length: > 1 } parts ? parts[1].Split('.') : [];
        Path = owner.Owner?.Provider?.FixPath(owner.Owner?.Name ?? path) ?? "N/A"; // full fixed .uasset path
    }

    public abstract void Dispose();

    public override string ToString() => Name;
}
