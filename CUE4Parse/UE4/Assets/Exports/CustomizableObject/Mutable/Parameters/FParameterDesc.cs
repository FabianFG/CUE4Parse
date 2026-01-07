using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FParameterDesc
{
    public string Name;
    public FGuid UID;
    public EParameterType Type;
    public object? DefaultValue;
    public uint[] Ranges;
    public FIntValueDesc[] PossibleValues;
   
    public FParameterDesc(FMutableArchive Ar)
    {
        Name = Ar.ReadFString();
        UID = Ar.Read<FGuid>();
        Type = Ar.Read<EParameterType>();

        var index = Ar.Read<byte>();
        DefaultValue = Type switch
        {
            EParameterType.None => null,
            EParameterType.Bool => Ar.ReadFlag(),
            EParameterType.Int => Ar.Read<int>(),
            EParameterType.Float => Ar.Read<float>(),
            EParameterType.Color => Ar.Read<FVector4>(),
            EParameterType.Projector => Ar.Read<FProjector>(),
            EParameterType.Texture => null,
            EParameterType.SkeletalMesh => null,
            EParameterType.Material => null,
            EParameterType.String => Ar.ReadFString(),
            EParameterType.Matrix => new FMatrix(Ar, false),
            EParameterType.InstancedStruct => null,
            _ => throw new NotSupportedException("Serialization for parameter type " + Type + " is not supported")
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

    /** Integer parameter type. It usually has a limited range of possible values that can be queried in the FParameters object. */
    Int,

    /** Floating point value in the range of 0.0 to 1.0 */
    Float,

    /** Floating point RGBA colour, with each channel ranging from 0.0 to 1.0 */
    Color,

    /** 3D Projector type, defining a position, scale and orientation.Basically used for projected decals. */
    Projector,

    /** An externally provided image. */
    Texture,

    /** An externally provided mesh. */
    SkeletalMesh,
		
    /** An externally provided material*/
    Material,

    /** A text string. */
    String,

    /** A 4x4 matrix. */
    Matrix,

    InstancedStruct,

    /** Utility enumeration value, not really a parameter type. */
    Count
}