using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawMachineLearnedBehavior : IRawBase
{
    public string[] MlControlNames;
    public RawLODMapping LODNeuralNetworkMapping;
    public RawMeshRegionMembership NeuralNetworkToMeshRegion;
    public RawNeuralNetwork[] NeuralNetworks;

    public RawMachineLearnedBehavior(FArchiveBigEndian Ar)
    {
        MlControlNames = Ar.ReadArray(Ar.ReadString);
        LODNeuralNetworkMapping = new RawLODMapping(Ar);
        NeuralNetworkToMeshRegion = new RawMeshRegionMembership(Ar);
        NeuralNetworks = Ar.ReadArray(() => new RawNeuralNetwork(Ar));
    }
}

public class RawMachineLearnedBehaviorExt : IRawBase
{
    public RawMachineLearnedBehaviorTypeData[] MLBTypeData;
    public RawMachineLearnedBehaviorJoints MLBJoints;

    public RawMachineLearnedBehaviorExt(FArchiveBigEndian Ar)
    {
        MLBTypeData = Ar.ReadArray(() => new RawMachineLearnedBehaviorTypeData(Ar));
        MLBJoints = new RawMachineLearnedBehaviorJoints(Ar);
    }
}

public class RawMachineLearnedBehaviorJoints
{
    public ushort[] ParameterKeys;
    public ushort[] ParameterValues;
    public ushort[] InputIndices;
    public ushort[] OutputIndices;

    public RawMachineLearnedBehaviorJoints(FArchiveBigEndian Ar)
    {
        ParameterKeys = Ar.ReadArray<ushort>();
        ParameterValues = Ar.ReadArray<ushort>();
        InputIndices = Ar.ReadArray<ushort>();
        OutputIndices = Ar.ReadArray<ushort>();
    }
}

public class RawMLOperation
{
    public uint[] Parameters;
    public ushort[] MLOperationSetDependencyIndices;
    public ushort[] MLOperationDependencyIndices;
    public EMachineLearnedBehaviorOperationType OperationType;

    public RawMLOperation(FArchiveBigEndian Ar)
    {
        Parameters = Ar.ReadArray<uint>();
        MLOperationSetDependencyIndices = Ar.ReadArray<ushort>();
        MLOperationDependencyIndices = Ar.ReadArray<ushort>();
        OperationType = (EMachineLearnedBehaviorOperationType) Ar.Read<ushort>();
    }
}

public class RawMachineLearnedBehaviorTypeData
{
    public RawLODMapping[] LodMLOperationMappings;
    public RawMLOperation[][] Operations;

    public RawMachineLearnedBehaviorTypeData(FArchiveBigEndian Ar)
    {
        LodMLOperationMappings = Ar.ReadArray(() => new RawLODMapping(Ar));
        Operations = Ar.ReadArray(() => Ar.ReadArray(() => new RawMLOperation(Ar)));
    }
}

[JsonConverter(typeof(EnumConverter<EMachineLearnedBehaviorOperationType>))]
public enum EMachineLearnedBehaviorOperationType : byte
{
    Unspecified = 0,
    Gather = 1,
    Scatter = 2,
    MLP = 3,
    Average = 4
}
