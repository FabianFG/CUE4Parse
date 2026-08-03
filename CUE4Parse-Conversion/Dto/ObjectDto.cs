using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.Dto;

public abstract class LightObjectDto(string name) : IDisposable
{
    public readonly string Name = name;

    protected LightObjectDto(UObject owner, string? name = null) : this(name ?? owner.Name)
    {

    }

    public abstract void Dispose();

    public override string ToString() => Name;
}

public abstract class ObjectDto : LightObjectDto
{
    public readonly string[] OuterNames;
    public readonly string Path;

    protected ObjectDto(UObject owner, string? name = null) : base(owner, name)
    {
        var path = owner.GetPathName();
        OuterNames = path.Split(':') is { Length: > 1 } parts ? parts[1].Split('.') : [];
        Path = owner.Owner?.Provider?.FixPath(owner.Owner?.Name ?? path) ?? "N/A"; // full fixed .uasset path
    }
}
