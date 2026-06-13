using System;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.Options;

public class ExportOptions(
    EMeshFormat meshFormat = EMeshFormat.USD,
    ENaniteMeshFormat naniteMeshFormat = ENaniteMeshFormat.NoNanite,
    EMeshQuality meshQuality = EMeshQuality.Highest,
    ETexturePlatform texturePlatform = ETexturePlatform.DesktopMobile,
    ETextureFormat textureFormat = ETextureFormat.Png,
    int textureQuality = 100,
    bool exportHdrTexturesAsHdr = true,
    EMaterialDepth materialDepth = EMaterialDepth.TopLayerOnly,
    bool exportMaterials = true,
    bool exportMorphTargets = true,
    ESocketFormat socketFormat = ESocketFormat.Bone,
    EFileCompressionFormat compressionFormat = EFileCompressionFormat.None)
{
    public readonly EMeshFormat MeshFormat = meshFormat;
    public readonly ENaniteMeshFormat NaniteMeshFormat = naniteMeshFormat;
    public readonly EMeshQuality MeshQuality = meshQuality;

    public readonly ETexturePlatform TexturePlatform = texturePlatform;
    public readonly ETextureFormat TextureFormat = meshFormat == EMeshFormat.USD ? ETextureFormat.Png : textureFormat; // USD pipeline requires PNG textures
    public readonly int TextureQuality = Math.Clamp(textureQuality, 1, 100);
    public readonly bool ExportHdrTexturesAsHdr = exportHdrTexturesAsHdr;

    public readonly EMaterialDepth MaterialDepth = materialDepth;
    public readonly bool ExportMaterials = exportMaterials; // not to be confused, when we export a mesh we will look (or not) for its materials and export them (or not)

    public readonly bool ExportMorphTargets = exportMorphTargets;
    public readonly ESocketFormat SocketFormat = socketFormat;

    public readonly EFileCompressionFormat CompressionFormat = meshFormat == EMeshFormat.UEFormat ? compressionFormat : EFileCompressionFormat.None;
}
