using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawDescriptor : IRawBase
{
    public string Name;
    public EArchetype Archetype;
    public EGender Gender;
    public ushort Age;
    public KeyValuePair<string, string>[] Metadata;
    public ETranslationUnit TranslationUnit;
    public ERotationUnit RotationUnit;
    public RawCoordinateSystem CoordinateSystem;
    public ushort LODCount;
    public ushort MaxLOD;
    public string Complexity;
    public string DBName;

    public RawDescriptor(FArchiveBigEndian Ar)
    {
        Name = Ar.ReadString();
        Archetype = (EArchetype) Ar.Read<ushort>();
        Gender = (EGender) Ar.Read<ushort>();
        Age = Ar.Read<ushort>();
        Metadata = Ar.ReadArray(() => new KeyValuePair<string, string>(Ar.ReadString(), Ar.ReadString()));
        TranslationUnit = (ETranslationUnit) Ar.Read<ushort>();
        RotationUnit = (ERotationUnit) Ar.Read<ushort>();
        CoordinateSystem = new RawCoordinateSystem(Ar);
        LODCount = Ar.Read<ushort>();
        MaxLOD = Ar.Read<ushort>();
        Complexity = Ar.ReadString();
        DBName = Ar.ReadString();
    }
}

public class RawDescriptorExt : IRawBase
{
    public ERotationSequence RotationSequence;
    public (ERotationDirection X, ERotationDirection Y, ERotationDirection Z) RotationSign;
    public EFaceWindingOrder FaceWindingOrder; //added in v28

    public RawDescriptorExt(FArchiveBigEndian Ar, FileVersion fileVersion)
    {
        RotationSequence = Ar.Read<ERotationSequence>();
        RotationSign = (Ar.Read<ERotationDirection>(), Ar.Read<ERotationDirection>(), Ar.Read<ERotationDirection>());
        FaceWindingOrder = fileVersion >= FileVersion.v28 ? Ar.Read<EFaceWindingOrder>() : EFaceWindingOrder.CCW;
    }
}

[JsonConverter(typeof(EnumConverter<EArchetype>))]
public enum EArchetype : byte
{
    Asian,
    Black,
    Caucasian,
    Hispanic,
    Alien,
    Other
}

[JsonConverter(typeof(EnumConverter<EGender>))]
public enum EGender : byte
{
    Male,
    Female,
    Other
}

[JsonConverter(typeof(EnumConverter<ETranslationUnit>))]
public enum ETranslationUnit : byte
{
    CM,
    M
}

[JsonConverter(typeof(EnumConverter<ERotationUnit>))]
public enum ERotationUnit : byte
{
    Degrees,
    Radians
}

[JsonConverter(typeof(EnumConverter<ERotationSequence>))]
public enum ERotationSequence
{
    XYZ,
    XZY,
    YXZ,
    YZX,
    ZXY,
    ZYX
}

[JsonConverter(typeof(EnumConverter<ERotationDirection>))]
public enum ERotationDirection : int
{
    None = 0,
    Positive = 1,
    Negative = -1
}

[JsonConverter(typeof(EnumConverter<EFaceWindingOrder>))]
public enum EFaceWindingOrder : byte
{
    CCW = 0,
    CW = 1
}
