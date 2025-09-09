using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawNeuralNetwork
{
    public uint Size;
    public uint BaseMarker;
    public ushort[] OutputIndices;
    public ushort[] InputIndices;
    public RawNeuralNetworkLayer[] Layers;
    public uint SizeMarker;

    public RawNeuralNetwork(FArchiveBigEndian Ar)
    {
        Size = Ar.Read<uint>();
        BaseMarker = Ar.Read<uint>();
        OutputIndices = Ar.ReadArray<ushort>();
        InputIndices = Ar.ReadArray<ushort>();
        Layers = Ar.ReadArray(() => new RawNeuralNetworkLayer(Ar));
        SizeMarker = Ar.Read<uint>();
    }
}
