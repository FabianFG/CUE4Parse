using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FProjector
{
    public EProjectorType Type;
    public FVector Position;
    public FVector Direction;
    public FVector Up;
    public FVector Scale;
    public float ProjectionAngle;
    
    public FProjector(FAssetArchive Ar)
    {
        Type = Ar.Read<EProjectorType>();
        Position = Ar.Read<FVector>();
        Direction = Ar.Read<FVector>();
        Up = Ar.Read<FVector>();
        Scale = Ar.Read<FVector>();
        ProjectionAngle = Ar.Read<float>();
    }
}

public enum EProjectorType : uint
{
    /** Standard projector that uses an affine transform. */
    Planar,

    /** Projector that wraps the projected image around a cylinder*/
    Cylindrical,

    /** Smart projector that tries to follow the projected surface geometry to minimize streching. */
    Wrapping,

    /** Utility enumeration value, not really a projector type. */
    Count
}