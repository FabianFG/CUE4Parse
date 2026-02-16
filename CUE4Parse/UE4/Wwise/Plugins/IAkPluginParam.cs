using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public interface IAkPluginParam;

public class CAkDefaultParams : IAkPluginParam
{
    public byte[] PluginData;

    public CAkDefaultParams(FArchive Ar, int size)
    {
        PluginData = Ar.ReadBytes(size);
    }
}
