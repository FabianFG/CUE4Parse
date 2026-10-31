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

    public FRotationSign(FStructFallback fallback) : this(fallback, new FRotationSign()) { }

    // Unversioned packages leave out the fields that match the defaults of the struct this one is a member of
    public FRotationSign(FStructFallback fallback, FRotationSign defaults)
    {
        XAxis = fallback.GetOrDefault(nameof(XAxis), defaults.XAxis);
        YAxis = fallback.GetOrDefault(nameof(YAxis), defaults.YAxis);
        ZAxis = fallback.GetOrDefault(nameof(ZAxis), defaults.ZAxis);
    }
}

[StructFallback]
public class FCoordinateSystem : IUStruct
{
    public EDirection XAxis;
    public EDirection YAxis;
    public EDirection ZAxis;

    public FCoordinateSystem() { }

    public FCoordinateSystem(FStructFallback fallback) : this(fallback, new FCoordinateSystem()) { }

    // Unversioned packages leave out the fields that match the defaults of the struct this one is a member of
    public FCoordinateSystem(FStructFallback fallback, FCoordinateSystem defaults)
    {
        XAxis = fallback.GetOrDefault(nameof(XAxis), defaults.XAxis);
        YAxis = fallback.GetOrDefault(nameof(YAxis), defaults.YAxis);
        ZAxis = fallback.GetOrDefault(nameof(ZAxis), defaults.ZAxis);
    }
}

[StructFallback]
public class FDNAConfig : IUStruct
{
    // Same defaults as UE, unversioned packages leave out the values that match them
    public int Layers = (int) EDNADataLayer.All;
    public FPerPlatformInt MaxLODPerPlatform = new();
    public FPerPlatformInt MinLODPerPlatform = new() { Default = -1 };
    public byte[] ExactLODs = [];
    public ECoordinateSystemTransformPolicy CoordinateSystemTransformPolicy = ECoordinateSystemTransformPolicy.Preserve;
    public FCoordinateSystem CoordinateSystem = new() { XAxis = EDirection.Left, YAxis = EDirection.Front, ZAxis = EDirection.Up };
    public FRotationSign RotationSign = new() { XAxis = ERotationDirection.Negative, YAxis = ERotationDirection.Negative, ZAxis = ERotationDirection.Positive };
    public ERotationSequence RotationSequence = ERotationSequence.XYZ;
    public EFaceWindingOrder FaceWindingOrder = EFaceWindingOrder.CW;

    public FDNAConfig() { }

    public FDNAConfig(FStructFallback fallback)
    {
        Layers = fallback.GetOrDefault(nameof(Layers), (int) EDNADataLayer.All);
        MaxLODPerPlatform = fallback.GetOrDefault(nameof(MaxLODPerPlatform), MaxLODPerPlatform);
        MinLODPerPlatform = fallback.GetOrDefault(nameof(MinLODPerPlatform), MinLODPerPlatform);
        ExactLODs = fallback.GetOrDefault<byte[]>(nameof(ExactLODs), []);
        CoordinateSystemTransformPolicy = fallback.GetOrDefault(nameof(CoordinateSystemTransformPolicy), ECoordinateSystemTransformPolicy.Preserve);
        if (fallback.TryGetValue(out FStructFallback coordinateSystem, nameof(CoordinateSystem)))
            CoordinateSystem = new FCoordinateSystem(coordinateSystem, CoordinateSystem);
        if (fallback.TryGetValue(out FStructFallback rotationSign, nameof(RotationSign)))
            RotationSign = new FRotationSign(rotationSign, RotationSign);
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
        Config = GetOrDefault(nameof(Config), new FDNAConfig());
    }
}
