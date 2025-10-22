using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Midi;

public enum EVariantTypes : int
{
    Empty = 0,
    Ansichar = 1,
    Bool = 2,
    Box = 3,
    BoxSphereBounds = 4,
    ByteArray = 5,
    Color = 6,
    DateTime = 7,
    Double = 8,
    Enum = 9,
    Float = 10,
    Guid = 11,
    Int8 = 12,
    Int16 = 13,
    Int32 = 14,
    Int64 = 15,
    IntRect = 16,
    LinearColor = 17,
    Matrix = 18,
    Name = 19,
    Plane = 20,
    Quat = 21,
    RandomStream = 22,
    Rotator = 23,
    String = 24,
    Widechar = 25,
    Timespan = 26,
    Transform = 27,
    TwoVectors = 28,
    UInt8 = 29,
    UInt16 = 30,
    UInt32 = 31,
    UInt64 = 32,
    Vector = 33,
    Vector2d = 34,
    Vector4 = 35,
    IntPoint = 36,
    IntVector = 37,
    NetworkGUID = 38,

    Custom = 0x40
};

public class FVariant : IUStruct
{
    public EVariantTypes Type;
    public byte[] Value;
    public FVariant(FAssetArchive Ar)
    {
        Type = Ar.Read<EVariantTypes>();
        Value = Ar.ReadArray<byte>();
    }
}