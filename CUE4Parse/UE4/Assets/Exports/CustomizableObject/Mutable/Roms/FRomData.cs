using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Roms;

public struct FRomData
{
    public uint Id;
    public uint SourceId;
    public uint Size;
    public uint ResourceIndex;
    public uint ResourceType;
    public ERomFlags Flags;

    public FRomData(FMutableArchive Ar)
    {
        Id = Ar.Read<uint>();
        SourceId = Ar.Game >= EGame.GAME_UE5_5 ? Ar.Read<uint>() : 0;
        Size = Ar.Read<uint>();
        ResourceIndex = Ar.Read<uint>();
        ResourceType = Ar.Game >= EGame.GAME_UE5_5 ? Ar.Read<ushort>() : Ar.Read<uint>();
        Flags = Ar.Game >= EGame.GAME_UE5_5 ? Ar.Read<ERomFlags>() : ERomFlags.None;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ERomFlags : ushort
{
    None = 0,
    HighRes = 1
}
