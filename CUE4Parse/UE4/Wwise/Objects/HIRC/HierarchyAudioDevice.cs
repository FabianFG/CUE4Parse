using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyAudioDevice : BaseHierarchyFx
{
    public AkFXParams FXParams { get; protected set; }

    public HierarchyAudioDevice(FArchive Ar) : base(Ar)
    {
        FXParams = new AkFXParams(Ar);
    }
}
