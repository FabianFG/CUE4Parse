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
    public EDataType ResourceType;
    public ERomFlags Flags;

    public FRomData(FMutableArchive Ar)
    {
        Id = Ar.Read<uint>();
        SourceId = Ar.Game >= EGame.GAME_UE5_5 ? Ar.Read<uint>() : 0;
        Size = Ar.Read<uint>();
        ResourceIndex = Ar.Read<uint>();
        ResourceType = (EDataType) (Ar.Game >= EGame.GAME_UE5_5 ? Ar.Read<ushort>() : Ar.Read<uint>());
        Flags = Ar.Game >= EGame.GAME_UE5_5 ? Ar.Read<ERomFlags>() : ERomFlags.None;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ERomFlags : ushort
{
    None = 0,
    HighRes = 1
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EDataType
{
    None = 0,
    Bool = 1,
    Int = 2,
    Scalar = 3,
    Colour = 4,
    Image = 5,
    VolumeDeprecated = 6,
    Layout = 7,
    Mesh = 8,
    Instance = 9,
    Projector = 10,
    String = 11,
    ExtensionData = 12, // Added in 5.3, so probably need to remap

    // Supporting data types: never returned as an actual data type for any operation.
    Matrix = 13,
    Shape = 14,
    Curve = 15,
    Skeleton = 16,
    PhysicsAsset = 17,

    Count = 18
}
