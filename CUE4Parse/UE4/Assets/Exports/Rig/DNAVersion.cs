using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class DNAVersion
{
    public ushort Generation;
    public ushort Version;
    public FileVersion FileVersion;

    public DNAVersion(FArchiveBigEndian Ar)
    {
        Generation = Ar.Read<ushort>();
        Version = Ar.Read<ushort>();
        FileVersion = (FileVersion)((Generation << 16) + Version);
    }
}

[JsonConverter(typeof(EnumConverter<FileVersion>))]
public enum FileVersion : ulong
{
    unknown = 0u,
    v10 = (1 << 16) + 0,
    v11 = (1 << 16) + 1,
    v21 = (2 << 16) + 1,
    v22 = (2 << 16) + 2,
    v23 = (2 << 16) + 3,
    v24 = (2 << 16) + 4,
    v25 = (2 << 16) + 5,
    latest = v25
}
