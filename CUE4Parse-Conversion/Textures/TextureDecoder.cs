using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures.ASTC;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.Textures.DXT;
using SkiaSharp;

namespace CUE4Parse_Conversion.Textures
{
    public static class TextureDecoder {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKImage? Decode(this UTexture2D texture) => texture.Decode(texture.GetFirstMip());

        public static SKImage? Decode(this UTexture2D texture, FTexture2DMipMap? mip)
        {
            if (!texture.IsVirtual && mip != null)
            {
                DecodeTexture(mip, texture.Format, out byte[] data, out var colorType);
                
                var width = mip.SizeX;
                var height = mip.SizeY;
                using var bitmap = new SKBitmap(new SKImageInfo(width, height, colorType, SKAlphaType.Unpremul));
                unsafe
                {
                    fixed (byte* p = data)
                    {
                        bitmap.SetPixels(new IntPtr(p));
                    }
                }

                return SKImage.FromBitmap(!texture.bRenderNearestNeighbor ? bitmap : bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.None));
            }
            return null;
        }

        private static void DecodeTexture(FTexture2DMipMap mip, EPixelFormat format, out byte[] data, out SKColorType colorType)
        {
            switch (format)
            {
                case EPixelFormat.PF_DXT1:
                    data = DXTDecoder.DXT1(mip.Data.Data, mip.SizeX, mip.SizeY, mip.SizeZ);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_DXT5:
                    data = DXTDecoder.DXT5(mip.Data.Data, mip.SizeX, mip.SizeY, mip.SizeZ);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ASTC_4x4:
                case EPixelFormat.PF_ASTC_6x6:
                case EPixelFormat.PF_ASTC_8x8:
                case EPixelFormat.PF_ASTC_10x10:
                case EPixelFormat.PF_ASTC_12x12:
                    data = ASTCDecoder.RGBA8888(
                        mip.Data.Data,
                        FormatHelper.GetBlockWidth(format),
                        FormatHelper.GetBlockHeight(format),
                        FormatHelper.GetBlockDepth(format),
                        mip.SizeX, mip.SizeY, mip.SizeZ);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_BC4:
                    data = BCDecoder.BC4(mip.Data.Data, mip.SizeX, mip.SizeY);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_BC5:
                    data = BCDecoder.BC5(mip.Data.Data, mip.SizeX, mip.SizeY);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_BC6H:
                    // BC6H doesn't work no matter the pixel format, the closest we can get is either
                    // Rgb565 DETEX_PIXEL_FORMAT_FLOAT_RGBX16 or Rgb565 DETEX_PIXEL_FORMAT_FLOAT_BGRX16
                    
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, true,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_FLOAT_RGBX16);
                    colorType = SKColorType.Rgb565;
                    break;
                case EPixelFormat.PF_BC7:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_ETC1:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC1,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ETC2_RGB:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ETC2_RGBA:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2_EAC,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_B8G8R8A8:
                    data = mip.Data.Data;
                    colorType = SKColorType.Bgra8888;
                    break;
                case EPixelFormat.PF_G8:
                    data = mip.Data.Data;
                    colorType = SKColorType.Gray8;
                    break;
                case EPixelFormat.PF_FloatRGBA:
                    data = mip.Data.Data;
                    colorType = SKColorType.RgbaF16;
                    break;
                default: throw new NotImplementedException($"Unknown pixel format: {format}");
            }
        }
    }
}
