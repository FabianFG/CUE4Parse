using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FParameterDesc
{
    public int Version;
    public string Name;
    public FGuid Uid;
    public EParameterType Type;
    public object DefaultValue;
    public uint[] Ranges;
    public FIntValueDesc[] PossibleValues;

    public FParameterDesc(FAssetArchive Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 10)
            throw new NotSupportedException($"Mutable FParameterDesc Version '{Version}' is currently not supported");

        Name = Ar.ReadMutableFString();
        Uid = Ar.Read<FGuid>();
        Type = Ar.Read<EParameterType>();

        Ar.Position += 1; // I guess TVariant stuff
        DefaultValue = Type switch
        {
            EParameterType.Bool => Ar.ReadBoolean(),
            EParameterType.Int => Ar.Read<int>(),
            EParameterType.Float => Ar.Read<float>(),
            EParameterType.Color => Ar.Read<FVector4>(),
            EParameterType.Projector => new FProjector(Ar),
            EParameterType.Image => Ar.ReadFName(),
            EParameterType.String => Ar.ReadMutableFString(),
            EParameterType.Matrix => Ar.Read<FMatrix>(),
            _ => throw new ParserException(Ar, $"Unknown EParameterType value '{Type}'")
            
        };
        
        Ranges = Ar.ReadArray<uint>();
        PossibleValues = Ar.ReadArray(() => new FIntValueDesc(Ar));
    }
}

public enum EParameterType : uint
{
    /** Undefined parameter type. */
    None,

    /** Boolean parameter type (true or false) */
    Bool,

    /** Integer parameter type. It usually has a limited range of possible values that can be queried in the Parameters object. */
    Int,

    /** Floating point value in the range of 0.0 to 1.0 */
    Float,

    /** Floating point RGBA colour, with each channel ranging from 0.0 to 1.0 */
    Color,

    /** 3D Projector type, defining a position, scale and orientation.Basically used for projected decals. */
    Projector,

    /** An externally provided image. */
    Image,

    /** A text string. */
    String,

    /** A 4x4 matrix. */
    Matrix,

    /** Utility enumeration value, not really a parameter type. */
    Count
}