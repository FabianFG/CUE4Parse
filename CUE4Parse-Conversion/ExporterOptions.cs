using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.PoseAsset;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.UE4.Assets.Exports.Nanite;

namespace CUE4Parse_Conversion;

public struct ExporterOptions()
{
    public ELodFormat LodFormat = ELodFormat.FirstLod;
    public EMeshFormat MeshFormat = EMeshFormat.UEFormat;
    public ENaniteMeshFormat NaniteMeshFormat = ENaniteMeshFormat.OnlyNaniteLOD;
    public EAnimFormat AnimFormat = EAnimFormat.UEFormat;
    public EPoseFormat PoseFormat;
    public EMaterialFormat MaterialFormat = EMaterialFormat.AllLayersNoRef;
    public ETextureFormat TextureFormat = ETextureFormat.Png;
    public EFileCompressionFormat CompressionFormat = EFileCompressionFormat.None;
    public ETexturePlatform Platform = ETexturePlatform.DesktopMobile;
    public ESocketFormat SocketFormat = ESocketFormat.Bone;
    public bool ExportMorphTargets = true;
    public bool ExportMaterials = true;
    public bool ExportHdrTexturesAsHdr = true;
}
