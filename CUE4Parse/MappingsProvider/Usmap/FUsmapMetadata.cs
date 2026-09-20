namespace CUE4Parse.MappingsProvider.Usmap;

public readonly struct FUsmapMetadata(FUsmapReader Ar)
{
    public readonly string Tool = Ar.ReadFString();
    public readonly string ToolVersion = Ar.ReadFString();
    public readonly long CreatedAtUnix = Ar.Read<long>();
}