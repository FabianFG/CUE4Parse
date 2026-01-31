using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkFxSrcSilence
{
    public static void Read(FArchive Ar, uint size, uint pluginId)
    {
        long maxOffset = Ar.Position + size;

        var fDuration = Ar.Read<float>();
        var fRandomizedLengthMinus = Ar.Read<float>();
        var fRandomizedLengthPlus = Ar.Read<float>();

        WwisePlugin.EnsureEndOfBlock(Ar, maxOffset, pluginId);
    }
}
