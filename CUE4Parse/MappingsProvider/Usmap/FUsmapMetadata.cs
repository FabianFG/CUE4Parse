using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider.Usmap;

public readonly struct FUsmapMetadata(FArchive Ar)
{
    public readonly string Tool = Ar.ReadFUtf8String();
    public readonly string ToolVersion = Ar.ReadFUtf8String();
    public readonly long CreatedAtUnix = Ar.Read<long>();
    public readonly EUsmapSource Source = Ar.Read<EUsmapSource>();
}

public enum EUsmapSource
{
    Runtime,
    MemoryDump,
    StaticAnalysis,
    Jmap,
    Custom
}