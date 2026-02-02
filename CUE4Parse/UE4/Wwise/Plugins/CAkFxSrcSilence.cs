using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkFxSrcSilence
{
    public readonly float Duration;
    public readonly float RandomizedLengthMinus;
    public readonly float RandomizedLengthPlus;

    public static object Read(FArchive Ar, uint size, EAkPluginId pluginId) =>
        new CAkFxSrcSilence(Ar, size, pluginId);

    public CAkFxSrcSilence(FArchive Ar, uint size, EAkPluginId pluginId)
    {
        long maxOffset = Ar.Position + size;

        Duration = Ar.Read<float>();
        RandomizedLengthMinus = Ar.Read<float>();
        RandomizedLengthPlus = Ar.Read<float>();

        WwisePlugin.EnsureEndOfBlock(Ar, maxOffset, pluginId);
    }
}
