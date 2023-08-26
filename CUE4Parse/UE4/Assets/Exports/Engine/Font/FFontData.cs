using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font
{
    [JsonConverter(typeof(FFontDataConverter))]
    public class FFontData : IUStruct
    {
        public FPackageIndex? LocalFontFaceAsset; // UObject
        public string? FontFilename;
        public EFontHinting Hinting;
        public EFontLoadingPolicy LoadingPolicy;
        public int SubFaceIndex;

        public FFontData(FAssetArchive Ar)
        {
            if (FEditorObjectVersion.Get(Ar) < FEditorObjectVersion.Type.AddedFontFaceAssets) return;

            var bIsCooked = Ar.ReadBoolean();
            if (bIsCooked)
            {
                LocalFontFaceAsset = new FPackageIndex(Ar);

                if (LocalFontFaceAsset == null)
                {
                    FontFilename = Ar.ReadFString();
                    Hinting = Ar.Read<EFontHinting>();
                    LoadingPolicy = Ar.Read<EFontLoadingPolicy>();
                }

                SubFaceIndex = Ar.Read<int>();
            }
        }
    }

    public enum EFontHinting : byte
    {
        /** Use the default hinting specified in the font. */
        Default,
        /** Force the use of an automatic hinting algorithm. */
        Auto,
        /** Force the use of an automatic light hinting algorithm, optimized for non-monochrome displays. */
        AutoLight,
        /** Force the use of an automatic hinting algorithm optimized for monochrome displays. */
        Monochrome,
        /** Do not use hinting. */
        None,
    }

    public enum EFontLoadingPolicy : byte
    {
        /** Lazy load the entire font into memory. This will consume more memory than Streaming, however there will be zero file-IO when rendering glyphs within the font, although the initial load may cause a hitch. */
        LazyLoad,
        /** Stream the font from disk. This will consume less memory than LazyLoad or Inline, however there will be file-IO when rendering glyphs, which may cause hitches under certain circumstances or on certain platforms. */
        Stream,
        /** Embed the font data within the asset. This will consume more memory than Streaming, however it is guaranteed to be hitch free (only valid for font data within a Font Face asset). */
        Inline,
    }
}
