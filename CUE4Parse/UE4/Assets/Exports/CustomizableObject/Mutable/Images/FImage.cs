using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;

public class FImage
{
    [JsonIgnore] public int Version = 4;
    public FImageDataStorage DataStorage;
    public EImageFlags Flags;
    
    public FImage(FMutableArchive Ar)
    {
        if (Ar.Game >= EGame.GAME_UE5_6) Version = Ar.Read<int>();

        DataStorage = new FImageDataStorage(Ar, Version);
        Flags = (EImageFlags) Ar.Read<byte>();
    }
}

[JsonConverter(typeof(StringEnumConverter))]
[Flags]
public enum EImageFlags
{
    IF_IS_None = 0,
    
    // Set if the next flag has been calculated and its value is valid.
    IF_IS_PLAIN_COLOUR_VALID = 1 << 0,
    
    // If the previous flag is set and this one too, the image is single color.
    IF_IS_PLAIN_COLOUR = 1 << 1,
    
    // If this is set, the image shouldn't be scaled: it's contents is resolution-dependent.
    IF_CANNOT_BE_SCALED = 1 << 2,
    
    // If this is set, the image has an updated relevancy map. This flag is not persistent.
    IF_HAS_RELEVANCY_MAP = 1 << 3,
    			
    /** For reference images, this indicates that they should be loaded into full images as soon as they are generated. */
    IF_IS_FORCELOAD = 1 << 5
}
