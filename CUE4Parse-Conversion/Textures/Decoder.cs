using CUE4Parse.UE4.Assets.Exports.Textures;
using CUE4Parse_Conversion.Textures.ASTC;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.Textures.DXT;
using SkiaSharp;
using System;

namespace CUE4Parse_Conversion.Textures
{
    public static class Decoder
    {
        public static SKImage? Decode(this UTexture2D texture)
        {
            if (!texture.IsVirtual && texture.GetFirstMip() is FTexture2DMipMap mip)
            {
                DecodeTexture(mip, texture.Format, out byte[] data, out SKColorType colorType);
                using var bitmap = new SKBitmap(new SKImageInfo(mip.SizeX, mip.SizeY, colorType, SKAlphaType.Unpremul));
                unsafe
                {
                    fixed (byte* p = data)
                    {
                        bitmap.SetPixels(new IntPtr(p));
                    }
                }
                return SKImage.FromBitmap(bitmap);
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
                case EPixelFormat.PF_ASTC_8x8:
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
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, isFloat: true,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBX8); // Not sure whether that works, would actually be DETEX_PIXEL_FORMAT_FLOAT_RGBX32
                    data = Detex.DecodeBC6H(data, mip.SizeX, mip.SizeY);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_BC7:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, isFloat: false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_ETC1:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, isFloat: false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC1,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ETC2_RGB:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, isFloat: false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ETC2_RGBA:
                    data = Detex.DecodeDetexLinear(mip.Data.Data, mip.SizeX, mip.SizeY, isFloat: false,
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
