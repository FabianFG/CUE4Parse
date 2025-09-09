using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawRBFSolver
{
    public uint Size;
    public uint BaseMarker;
    public string Name;
    public ushort[] RawControlIndices;
    public ushort[] PoseIndices;
    public float[] RawControlValues;
    public float Radius;
    public float WeightThreshold;
    public ushort SolverType; // cld be ERBFSolverType
    public EAutomaticRadius AutomaticRadius;
    public ushort DistanceMethod;
    public ushort NormalizeMethod;
    public ushort FunctionType;
    public ETwistAxis TwistAxis;
    public uint SizeMarker;

    public RawRBFSolver(FArchiveBigEndian Ar)
    {
        Size = Ar.Read<uint>();
        BaseMarker = Ar.Read<uint>();
        Name = Ar.ReadString();
        RawControlIndices = Ar.ReadArray<ushort>();
        PoseIndices = Ar.ReadArray<ushort>();
        RawControlValues = Ar.ReadArray<float>();
        Radius = Ar.Read<float>();
        WeightThreshold = Ar.Read<float>();
        SolverType = Ar.Read<ushort>();
        AutomaticRadius = (EAutomaticRadius) Ar.Read<ushort>();
        DistanceMethod = Ar.Read<ushort>();
        NormalizeMethod = Ar.Read<ushort>();
        FunctionType = Ar.Read<ushort>();
        TwistAxis = (ETwistAxis) Ar.Read<ushort>();
        SizeMarker = Ar.Read<uint>();
    }
}

public enum EAutomaticRadius : byte
{
    On,
    Off
}

public enum ETwistAxis : byte
{
    X,
    Y,
    Z
}
