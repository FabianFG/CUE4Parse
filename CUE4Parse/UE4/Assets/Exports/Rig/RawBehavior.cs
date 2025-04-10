using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawBehavior
{
    public RawControls Controls;
    public RawJoints Joints;
    public RawBlendShapeChannels BlendShapeChannels;
    public RawAnimatedMaps AnimatedMaps;

    public RawBehavior(FArchiveBigEndian Ar, SectionLookupTable sections, long startPos)
    {
        Ar.Position = startPos + sections.Controls;
        Controls = new RawControls(Ar);

        Ar.Position = startPos + sections.Joints;
        Joints = new RawJoints(Ar);

        Ar.Position = startPos + sections.BlendShapeChannels;
        BlendShapeChannels = new RawBlendShapeChannels(Ar);

        Ar.Position = startPos + sections.AnimatedMaps;
        AnimatedMaps = new RawAnimatedMaps(Ar);
    }
}
