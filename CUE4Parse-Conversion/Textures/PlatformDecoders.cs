using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.Textures.DXT;
using SkiaSharp;

namespace CUE4Parse_Conversion.Textures
{
    public static class PlaystationDecoder
    {
        public static void DecodeTexturePlaystation(FTexture2DMipMap mip, EPixelFormat format, bool isNormalMap, out byte[] data, out SKColorType colorType)
        {
            switch (format)
            {
                case EPixelFormat.PF_DXT5:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 16;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    var d = PlatformDeswizzlers.DeswizzlePS4(mip.BulkData.Data, mip.SizeX, mip.SizeY, 4, 4, 16);
                    data = DXTDecoder.DXT5(d, mip.SizeX, mip.SizeY, mip.SizeZ);
                    colorType = SKColorType.Rgba8888;
                    break;
                }
                case EPixelFormat.PF_DXT1:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 8;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    var d = PlatformDeswizzlers.DeswizzlePS4(mip.BulkData.Data, mip.SizeX, mip.SizeY, 4, 4, 8);
                    data = DXTDecoder.DXT1(d, mip.SizeX, mip.SizeY, mip.SizeZ);
                    colorType = SKColorType.Rgba8888;
                    break;
                }
                case EPixelFormat.PF_B8G8R8A8:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 4;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    data = PlatformDeswizzlers.DeswizzlePS4(mip.BulkData.Data, mip.SizeX, mip.SizeY, 1, 1, 4);
                    colorType = SKColorType.Bgra8888;
                    break;
                }
                case EPixelFormat.PF_G8:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 1;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    data = PlatformDeswizzlers.DeswizzlePS4(mip.BulkData.Data, mip.SizeX, mip.SizeY, 1, 1, 1);
                    colorType = SKColorType.Gray8;
                    break;
                }
                case EPixelFormat.PF_BC5:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 16;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    var d = PlatformDeswizzlers.DeswizzlePS4(mip.BulkData.Data, mip.SizeX, mip.SizeY, 4, 4, 16);
                    data = BCDecoder.BC5(d, mip.SizeX, mip.SizeY);
                    colorType = SKColorType.Rgb888x;
                    break;
                }
                case EPixelFormat.PF_BC4:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 8;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    var d = PlatformDeswizzlers.DeswizzlePS4(mip.BulkData.Data, mip.SizeX, mip.SizeY, 4, 4, 8);
                    data = BCDecoder.BC4(d, mip.SizeX, mip.SizeY);
                    colorType = SKColorType.Rgb888x;
                    break;
                }
                default:
                {
                    TextureDecoder.DecodeTexture(mip, format, isNormalMap, ETexturePlatform.DesktopMobile, out data, out colorType);
                    break;
                }
            }
        }
    }

    // NOTE: The deswizzling for Nintendo Switch only works for square textures, non-square textures require more investigation
    public static class NintendoSwitchDecoder
    {
        public static void DecodeTextureNSW(FTexture2DMipMap mip, EPixelFormat format, bool isNormalMap, out byte[] data, out SKColorType colorType)
        {
            switch (format)
            {
                case EPixelFormat.PF_DXT5:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 16;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    var d = PlatformDeswizzlers.DesizzleNSW(mip.BulkData.Data, mip.SizeX, mip.SizeY, 4, 4, 16);
                    data = DXTDecoder.DXT5(d, mip.SizeX, mip.SizeY, mip.SizeZ);
                    colorType = SKColorType.Rgba8888;
                    break;
                }
                case EPixelFormat.PF_DXT1:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 8;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    var d = PlatformDeswizzlers.DesizzleNSW(mip.BulkData.Data, mip.SizeX, mip.SizeY, 4, 4, 8);
                    data = DXTDecoder.DXT1(d, mip.SizeX, mip.SizeY, mip.SizeZ);
                    colorType = SKColorType.Rgba8888;
                    break;
                }
                case EPixelFormat.PF_B8G8R8A8:
                {
                    var uBlockSize = mip.SizeX / 4;
                    var vBlockSize = mip.SizeY / 4;
                    var totalBlocks = mip.BulkData.Data.Length / 4;

                    if (uBlockSize * vBlockSize > totalBlocks)
                    {
                        throw new ParserException($"Texture unable to be untiled: {format}");
                    }

                    data = PlatformDeswizzlers.DesizzleNSW(mip.BulkData.Data, mip.SizeX, mip.SizeY, 1, 1, 4);
                    colorType = SKColorType.Bgra8888;
                    break;
                }
                default:
                {
                    TextureDecoder.DecodeTexture(mip, format, isNormalMap, ETexturePlatform.DesktopMobile, out data, out colorType);
                    break;
                }
            }
        }
    }
}
