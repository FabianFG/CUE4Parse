using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Image;

public class FImage : IMutablePtr
{
    public FImageDataStorage DataStorage;
    public EImageFlags Flags;

    public bool IsBroken { get; set; }

    public FImage(FArchive Ar)
    {
        var version = Ar.Read<int>();
        if (version == -1)
        {
            IsBroken = true;
            return;
        }

        DataStorage = new FImageDataStorage(Ar);
        Flags = Ar.Read<EImageFlags>();
    }
}

public enum EImageFlags : byte
{
    IF_IS_PLAIN_COLOUR_VALID = 1 << 0,

    // If the previous flag is set and this one too, the image is single colour.
    IF_IS_PLAIN_COLOUR = 1 << 1,

    // If this is set, the image shouldn't be scaled: it's contents is resoultion-dependent.
    IF_CANNOT_BE_SCALED = 1 << 2,

    // If this is set, the image has an updated relevancy map. This flag is not persisent.
    IF_HAS_RELEVANCY_MAP = 1 << 3,

    /** If this is set, this is a reference to an external image, and the ReferenceID is valid. */
    IF_IS_REFERENCE = 1 << 4,

    /** For reference images, this indicates that they should be loaded into full images as soon as they are generated. */
    IF_IS_FORCELOAD = 1 << 5
}
