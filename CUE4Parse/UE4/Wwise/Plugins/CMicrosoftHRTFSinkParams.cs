using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CMicrosoftHRTFSinkParams(FArchive Ar) : IAkPluginParam
{
    public float RoomSize = Ar.Read<float>();
}
