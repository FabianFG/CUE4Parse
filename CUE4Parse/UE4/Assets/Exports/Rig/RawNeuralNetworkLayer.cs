using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawNeuralNetworkLayer
{
    public float[] Biases;
    public float[] Weights;
    public RawActivationFunction ActivationFunction;

    public RawNeuralNetworkLayer(FArchiveBigEndian Ar)
    {
        Biases = Ar.ReadArray<float>();
        Weights = Ar.ReadArray<float>();
        ActivationFunction = new RawActivationFunction(Ar);
    }
}
