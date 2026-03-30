using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FParameterDesc
{
    [JsonIgnore] public int Version = 10;
    public string Name;
    // Unique id (provided externally, so no actual guarantee that it is unique.)
    public FGuid UID;
    public EParameterType Type;
    public object? DefaultValue;
    // Ranges, if the parameter is multi-dimensional. The indices refer to the Model's program vector of range descriptors.
    public uint[] Ranges;
    // For integer parameters, this contains the description of the possible values. If empty, the integer may have any value.
    public FIntValueDesc[] PossibleValues;
   
    public FParameterDesc(FMutableArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_6) Version = Ar.Read<int>();

        if (Ar.Game >= EGame.GAME_UE5_4)
        {
            Name = Ar.ReadFString();
            UID = Ar.Read<FGuid>();
        }
        else
        {
            Name = Ar.ReadString();
            UID = new FGuid(Ar.ReadString());
        }

        Type = ReadMaterialParameterType(Ar);

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
            EParameterType.String => Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadFString() : Ar.ReadString(),
            EParameterType.Matrix => new FMatrix(Ar, false),
            EParameterType.InstancedStruct => null,
            // for older versions
            EParameterType.Image => Ar.Game >= EGame.GAME_UE5_4 ? Ar.ReadFString() : Ar.ReadString(),
            _ => throw new NotSupportedException("Serialization for parameter type " + Type + " is not supported")
        };

        Ranges = Ar.ReadArray<uint>();
        if (Version < 6)
            Ar.SkipFixedArray(sizeof(int)); // UnusedDescImages
        PossibleValues = Ar.ReadArray(() => new FIntValueDesc(Ar));
    }

    public EParameterType ReadMaterialParameterType(FArchive Ar)
    {
        var value = Ar.Read<uint>();
        return Ar.Game switch
        {
            >= EGame.GAME_UE5_7 => (EParameterType) value,
            >= EGame.GAME_UE5_6 => value switch
            {
                >= 10 => throw new ParserException("Unsupported parameter type"),
                9 => EParameterType.Matrix,
                8 => EParameterType.String,
                7 => EParameterType.SkeletalMesh,
                6 => EParameterType.Image,
                _ => (EParameterType) value,
            },
            >= EGame.GAME_UE5_5 => value switch
            {
                >= 9 => throw new ParserException("Unsupported parameter type"),
                8 => EParameterType.Matrix,
                7 => EParameterType.String,
                6 => EParameterType.Image,
                _ => (EParameterType) value,
            },
            _ => value switch
            {
                >= 8 => throw new ParserException("Unsupported parameter type"),
                7 => EParameterType.String,
                6 => EParameterType.Image,
                _ => (EParameterType) value,
            }
        };
    }
}

[JsonConverter(typeof(StringEnumConverter))]
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
    Count,

    // used in older versions
    Image,
}
