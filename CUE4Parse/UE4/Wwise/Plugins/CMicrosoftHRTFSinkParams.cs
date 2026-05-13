namespace CUE4Parse.UE4.Wwise.Plugins;

public class CMicrosoftHRTFSinkParams(FWwiseArchive Ar) : IAkPluginParam
{
    public float RoomSize = Ar.Read<float>();
}
