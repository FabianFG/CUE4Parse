using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Sound.Node;

public class USoundNodeDoppler : USoundNode;
public class USoundNodeAttenuation : USoundNode;
public class USoundNodeQualityLevel : USoundNode;
public class USoundNodeEnveloper : USoundNode;
public class USoundNodeDelay : USoundNode;
public class USoundNodeMixer : USoundNode
{
    public float[] InputVolume;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        InputVolume = GetOrDefault<float[]>(nameof(InputVolume), []);
    }
}
public class USoundNodeModulator : USoundNode;
public class USoundNodeRandom : USoundNode
{
    public float[] Weights;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Weights = GetOrDefault<float[]>(nameof(Weights), []);
    }
}
public class USoundNodeDistanceCrossFade : USoundNode;
public class USoundNodeSwitch : USoundNode;
public class USoundNodeModulatorContinuous : USoundNode;
public class USoundNodeSoundClass : USoundNode;
public class USoundNodeParamCrossFade : USoundNode;
public class USoundNodeLooping : USoundNode;
public class USoundNodeBranch : USoundNode;
