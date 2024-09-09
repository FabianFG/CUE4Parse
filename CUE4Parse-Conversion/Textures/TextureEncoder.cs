using System.IO;
using SkiaSharp;

namespace CUE4Parse_Conversion.Textures;

public static class TextureEncoder
{
    public static SKData Encode(this SKBitmap bitmap, ETextureFormat format, int quality)
    {
        if (format == ETextureFormat.Png) return bitmap.Encode(SKEncodedImageFormat.Png, quality);
        if (format == ETextureFormat.Jpeg) return bitmap.Encode(SKEncodedImageFormat.Jpeg, quality);

        // TGA really doesn't support texture quality levels
        if (format == ETextureFormat.Tga)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var header = new byte[18];
            header[2] = 2; // uncompressed
            header[12] = (byte) (bitmap.Width & 0xFF);
            header[13] = (byte) (bitmap.Width >> 8);
            header[14] = (byte) (bitmap.Height & 0xFF);
            header[15] = (byte) (bitmap.Height >> 8);
            header[16] = 32; // 32 bit
            header[17] = 32; // 8 bits of alpha
            writer.Write(header);

            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    writer.Write(color.Blue);
                    writer.Write(color.Green);
                    writer.Write(color.Red);
                    writer.Write(color.Alpha);
                }
            }

            return SKData.CreateCopy(stream.ToArray());
        }

        return SKData.Empty;
    }
}
