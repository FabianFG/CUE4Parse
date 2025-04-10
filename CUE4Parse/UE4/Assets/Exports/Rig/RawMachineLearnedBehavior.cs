using CUE4Parse.UE4.Readers;

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
