using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public enum EDirection : byte
{
    Left,
    Right,
    Up,
    Down,
    Front,
    Back
}

public enum EDNADataLayer : int
{
    None = 0,
    Descriptor = 1,
    Definition = 2 | Descriptor,
    Behavior = 4 | Definition,
    Geometry = 8 | Definition,
    GeometryWithoutBlendShapes = 16 | Definition,
    MachineLearnedBehavior = 32 | Definition,
    RBFBehavior = 64 | Behavior,
    All = RBFBehavior | Geometry | MachineLearnedBehavior
}

public enum ECoordinateSystemTransformPolicy : byte
{
    Preserve,
    Transform
}

[StructFallback]
public class FRotationSign : IUStruct
{
    public ERotationDirection XAxis;
    public ERotationDirection YAxis;
    public ERotationDirection ZAxis;

    public FRotationSign() { }

    public FRotationSign(FStructFallback fallback)
    {
        XAxis = fallback.GetOrDefault<ERotationDirection>(nameof(XAxis));
        YAxis = fallback.GetOrDefault<ERotationDirection>(nameof(YAxis));
        ZAxis = fallback.GetOrDefault<ERotationDirection>(nameof(ZAxis));
    }
}

[StructFallback]
public class FCoordinateSystem : IUStruct
{
    public EDirection XAxis;
    public EDirection YAxis;
    public EDirection ZAxis;

    public FCoordinateSystem() { }

    public FCoordinateSystem(FStructFallback fallback)
    {
        XAxis = fallback.GetOrDefault<EDirection>(nameof(XAxis));
        YAxis = fallback.GetOrDefault<EDirection>(nameof(YAxis));
        ZAxis = fallback.GetOrDefault<EDirection>(nameof(ZAxis));
    }
}

[StructFallback]
public class FDNAConfig : IUStruct
{
    public int Layers = (int) EDNADataLayer.All;
    public FPerPlatformInt MaxLODPerPlatform;
    public FPerPlatformInt MinLODPerPlatform;
    public byte[] ExactLODs = [];
    public ECoordinateSystemTransformPolicy CoordinateSystemTransformPolicy = ECoordinateSystemTransformPolicy.Preserve;
    public FCoordinateSystem CoordinateSystem = new();
    public FRotationSign RotationSign = new();
    public ERotationSequence RotationSequence = ERotationSequence.XYZ;
    public EFaceWindingOrder FaceWindingOrder = EFaceWindingOrder.CW;

    public FDNAConfig(FStructFallback fallback)
    {
        Layers = fallback.GetOrDefault(nameof(Layers), (int) EDNADataLayer.All);
        MaxLODPerPlatform = fallback.GetOrDefault<FPerPlatformInt>(nameof(MaxLODPerPlatform));
        MinLODPerPlatform = fallback.GetOrDefault<FPerPlatformInt>(nameof(MinLODPerPlatform));
        ExactLODs = fallback.GetOrDefault<byte[]>(nameof(ExactLODs), []);
        CoordinateSystemTransformPolicy = fallback.GetOrDefault(nameof(CoordinateSystemTransformPolicy), ECoordinateSystemTransformPolicy.Preserve);
        CoordinateSystem = fallback.GetOrDefault(nameof(CoordinateSystem), new FCoordinateSystem());
        RotationSign = fallback.GetOrDefault(nameof(RotationSign), new FRotationSign());
        RotationSequence = fallback.GetOrDefault(nameof(RotationSequence), ERotationSequence.XYZ);
        FaceWindingOrder = fallback.GetOrDefault(nameof(FaceWindingOrder), EFaceWindingOrder.CW);
    }
}

public class UDNAConfigHolder : UObject
{
    public FDNAConfig Config;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Config = GetOrDefault<FDNAConfig>(nameof(Config));
    }
}
