namespace CUE4Parse.UE4.Wwise.Plugins;

public interface IAkPluginParam;

public class CAkDefaultParams(FWwiseArchive Ar, int size) : IAkPluginParam
{
    public byte[] PluginData = Ar.ReadBytes(size);
}
