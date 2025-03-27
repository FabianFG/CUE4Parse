using System;
using System.IO;
using System.Linq;
using System.Text;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using OffiUtils;
using SkiaSharp;

namespace CUE4Parse_Conversion.Textures;

public static class TextureEncoder
{
    public static byte[] Encode(this CTexture bitmap, ETextureFormat format, out string ext)
    {
        //always export float data as HDR
        if (PixelFormatUtils.IsHDR(bitmap.PixelFormat))
        {
            ext = "hdr";
            return ToHdrBitmap(bitmap);
        }

        switch (format)
        {
            case ETextureFormat.Png:
            {
                ext = "png";
                using var bmp = bitmap.ToSkBitmap();
                using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            case ETextureFormat.Jpeg:
            {
                ext = "jpg";
                using var bmp = bitmap.ToSkBitmap();
                using var data = bmp.Encode(SKEncodedImageFormat.Jpeg, 100);
                return data.ToArray();
            }
            case ETextureFormat.Tga:
                ext = "tga";
                return EncodeTga(bitmap);
            default:
                ext = "unk";
                return [];
            //TODO: ETextureFormat.Dds
        }
    }

    private static byte[] EncodeTga(CTexture bitmap)
    {
        using var skBitmap = bitmap.ToSkBitmap();
        int width = skBitmap.Width;
        int height = skBitmap.Height;
        int pixelDataSize = width * height * 4;
        int totalSize = 18 + pixelDataSize;

        byte[] output = new byte[totalSize];

        //TGA header
        output[2] = 2; // Uncompressed
        output[12] = (byte)(width & 0xFF);
        output[13] = (byte)(width >> 8);
        output[14] = (byte)(height & 0xFF);
        output[15] = (byte)(height >> 8);
        output[16] = 32; // 32-bit
        output[17] = 8;  // 8 bits of alpha

        unsafe
        {
            fixed (byte* ptr = output)
            {
                byte* pixelPtr = ptr + 18; //Start writing after header

                for (int y = height - 1; y >= 0; y--)//TGA stores pixels bottom-up
                {
                    for (int x = 0; x < width; x++)
                    {
                        var color = skBitmap.GetPixel(x, y);
                        *pixelPtr++ = color.Blue;
                        *pixelPtr++ = color.Green;
                        *pixelPtr++ = color.Red;
                        *pixelPtr++ = color.Alpha;
                    }
                }
            }
        }
        return output;
    }

#region Hdr

    public static byte[] ToHdrBitmap(this CTexture texture)
    {
        var dataSpan = new ReadOnlySpan<byte>(texture.Data);
        nint convertedDataPtr = nint.Zero;

        // Convert the texture data to RGBE format
        switch (texture.PixelFormat)
        {
            case EPixelFormat.PF_R16F:
            case EPixelFormat.PF_FloatRGB:
            case EPixelFormat.PF_FloatRGBA:
                convertedDataPtr = ConvertHalfToRGBE(texture.PixelFormat, texture.Width, texture.Height, dataSpan);
                break;
            case EPixelFormat.PF_R32_FLOAT:
            case EPixelFormat.PF_R32G32B32F:
                convertedDataPtr = ConvertFloatToRGBE(texture.PixelFormat, texture.Width, texture.Height, dataSpan);
                break;
            case EPixelFormat.PF_A32B32G32R32F:
                convertedDataPtr = ConvertFloatToRGBE(texture.PixelFormat, texture.Width, texture.Height, dataSpan, true);
                break;
            default:
                throw new NotImplementedException("Unsupported pixel format: " + texture.PixelFormat);
        }

        if (convertedDataPtr == nint.Zero)
            throw new Exception("Failed to convert texture data to RGBE format.");

        using var stream = new MemoryStream();

        //Write the HDR header
        WriteHdrHeader(stream, texture.Width, texture.Height);

        //Process each scanline directly from convertedDataPtr
        for (int y = 0; y < texture.Height; y++)
        {
            int rowOffset = y * texture.Width * 4;
            WriteScanlineRLE(stream, convertedDataPtr, texture.Width, rowOffset);
        }

        // Free allocated native memory
        MemoryUtils.NativeFree(convertedDataPtr);

        return stream.ToArray();
    }

    private static void WriteHdrHeader(Stream stream, int width, int height)
    {
        stream.Write("#?RADIANCE\n# Written by CUE4Parse\nFORMAT=32-bit_rle_rgbe\n\n"u8);
        stream.Write(Encoding.ASCII.GetBytes($"-Y {height} +X {width}\n"));
    }

    private static unsafe nint ConvertFloatToRGBE(EPixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> inp, bool flipOrder = false)
    {
        int channelCount = PixelFormatUtils.PixelFormats.First(x => x.UnrealFormat == pixelFormat).NumComponents;

        MemoryUtils.NativeAlloc<byte>(width * height * 4, out var retPtr);

        fixed (byte* inpPtr = inp)
        {
            byte* outPtr = (byte*)retPtr;

            for (int i = 0; i < width * height; i++)
            {
                int pixelOffset = i * channelCount * sizeof(float); //4 bytes

                float r = 0, g = 0, b = 0;

                if (channelCount == 1)
                {
                    // If there's only 1 channel, we just take that value for r, g, and b
                    r = g = b = *(float*)(inpPtr + pixelOffset);
                }
                else
                {
                    for (int c = 0; c < channelCount; c++)
                    {
                        int offset = flipOrder ? (channelCount - c - 1) * sizeof(float) : c * sizeof(float);
                        float channelValue = *(float*)(inpPtr + pixelOffset + offset);

                        if (c == 0)
                            r = channelValue;  // Red
                        else if (c == 1)
                            g = channelValue;  // Green
                        else if (c == 2)
                            b = channelValue;  // Blue
                    }
                }

                // Find the max component for RGBE scaling
                float maxValue = Math.Max(r, Math.Max(g, b));

                byte rByte, gByte, bByte, exponentByte;
                if (maxValue < 1e-32f)
                {
                    rByte = gByte = bByte = exponentByte = 0;
                }
                else
                {
                    int exponent = (int)Math.Floor(Math.Log2(maxValue)) + 1;
                    float scaleFactor = 256.0f / MathF.Pow(2, exponent);

                    rByte = (byte)(r * scaleFactor);
                    gByte = (byte)(g * scaleFactor);
                    bByte = (byte)(b * scaleFactor);
                    exponentByte = (byte)(exponent + 128);
                }

                // Store result in RGBE format
                int idx = i * 4;
                outPtr[idx] = rByte;
                outPtr[idx + 1] = gByte;
                outPtr[idx + 2] = bByte;
                outPtr[idx + 3] = exponentByte;
            }
        }

        return retPtr;
    }

    private static unsafe nint ConvertHalfToRGBE(EPixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> inp, bool flipOrder = false)
    {
        int channelCount = PixelFormatUtils.PixelFormats.First(x => x.UnrealFormat == pixelFormat).NumComponents;

        MemoryUtils.NativeAlloc<byte>(width * height * 4, out var retPtr);

        fixed (byte* inpPtr = inp)
        {
            byte* outPtr = (byte*)retPtr;

            for (int i = 0; i < width * height; i++)
            {
                int pixelOffset = i * channelCount * sizeof(Half); //4 bytes

                float r = 0, g = 0, b = 0;

                if (channelCount == 1)
                {
                    // If there's only 1 channel, we just take that value for r, g, and b
                    r = g = b = (float)*(Half*)(inpPtr + pixelOffset);
                }
                else
                {
                    for (int c = 0; c < channelCount; c++)
                    {
                        int offset = flipOrder ? (channelCount - c - 1) * sizeof(Half) : c * sizeof(Half);
                        float channelValue = (float)*(Half*)(inpPtr + pixelOffset + offset);

                        if (c == 0)
                            r = channelValue;  // Red
                        else if (c == 1)
                            g = channelValue;  // Green
                        else if (c == 2)
                            b = channelValue;  // Blue
                    }
                }

                // Find the max component for RGBE scaling
                float maxValue = Math.Max(r, Math.Max(g, b));

                byte rByte, gByte, bByte, exponentByte;
                if (maxValue < 1e-32f)
                {
                    rByte = gByte = bByte = exponentByte = 0;
                }
                else
                {
                    int exponent = (int)Math.Floor(Math.Log2(maxValue)) + 1;
                    float scaleFactor = 256.0f / MathF.Pow(2, exponent);

                    rByte = (byte)(r * scaleFactor);
                    gByte = (byte)(g * scaleFactor);
                    bByte = (byte)(b * scaleFactor);
                    exponentByte = (byte)(exponent + 128);
                }

                // Store result in RGBE format
                int idx = i * 4;
                outPtr[idx] = rByte;
                outPtr[idx + 1] = gByte;
                outPtr[idx + 2] = bByte;
                outPtr[idx + 3] = exponentByte;
            }
        }

        return retPtr;
    }

    private static void WriteScanlineRLE(MemoryStream stream, nint dataPtr, int scanlineWidth, int rowOffset)
    {
        // Write the scanline header (scanline type 2 and width)
        stream.WriteByte(2);
        stream.WriteByte(2);
        stream.WriteByte((byte)(scanlineWidth >> 8));
        stream.WriteByte((byte)(scanlineWidth & 0xFF));

        // Process each channel (RGBA) separately
        for (int channel = 0; channel < 4; channel++)
        {
            WriteChannelRLE(stream, dataPtr, scanlineWidth, rowOffset, channel);
        }
    }

    private static void WriteChannelRLE(MemoryStream stream, nint dataPtr, int scanlineWidth, int rowOffset, int channel)
    {
        int current = 0;

        while (current < scanlineWidth)
        {
            unsafe
            {
                // Get a pointer to the current channel byte in the pixel data
                byte* pixelPtr = (byte*)dataPtr + rowOffset + (current * 4) + channel;

                // Try to find a run of identical values for the current channel
                int runLength = 1;

                // Check for identical values in the current channel
                while (current + runLength < scanlineWidth && runLength < 127 && *(pixelPtr) == *(pixelPtr + (runLength * 4)))
                {
                    runLength++;
                }

                // If we found a run of 4 or more identical values, encode the run using RLE
                if (runLength >= 4)
                {
                    stream.WriteByte((byte)(128 + runLength)); // Start of RLE, 128 + runLength
                    stream.WriteByte(*(pixelPtr)); // Value to repeat
                    current += runLength;
                }
                else
                {
                    // If it's not a run, write individual values
                    int nonRunLength = 1;
                    while (current + nonRunLength < scanlineWidth && nonRunLength < 128)
                    {
                        int nextRun = 1;
                        while (current + nonRunLength + nextRun < scanlineWidth && nextRun < 127 &&
                               *(pixelPtr + (nonRunLength * 4)) == *(pixelPtr + ((nonRunLength + nextRun) * 4)))
                        {
                            nextRun++;
                        }

                        // Break early if a run is detected
                        if (nextRun >= 4)
                            break;

                        nonRunLength++;
                    }

                    // Write the non-run length values
                    stream.WriteByte((byte)nonRunLength);
                    for (int i = 0; i < nonRunLength; i++)
                    {
                        stream.WriteByte(*(pixelPtr + (i * 4)));
                    }
                    current += nonRunLength;
                }
            }
        }
    }

#endregion

#region SkBitmap

    public static SKBitmap ToSkBitmap(this CTexture texture)
    {
        var dataSpan = new ReadOnlySpan<byte>(texture.Data);
        var convertedData = nint.Zero;

        SKColorType skColorType = SKColorType.Rgba8888;
        switch (texture.PixelFormat)
        {
            case EPixelFormat.PF_R8G8B8A8:
                break;
            case EPixelFormat.PF_B8G8R8A8:
                skColorType = SKColorType.Bgra8888;
                break;
            case EPixelFormat.PF_R8:
                skColorType = SKColorType.Gray8;
                break;
            case EPixelFormat.PF_FloatRGB:
                convertedData = ConvertHalfTo8(texture.PixelFormat, texture.Width, texture.Height, dataSpan);
                break;
            case EPixelFormat.PF_FloatRGBA:
                convertedData = ConvertHalfTo8(texture.PixelFormat, texture.Width, texture.Height, dataSpan);
                break;
            case EPixelFormat.PF_R32_FLOAT:
            case EPixelFormat.PF_R32G32B32F:
                convertedData = ConvertFloatTo8(texture.PixelFormat, texture.Width, texture.Height, dataSpan);
                break;
            case EPixelFormat.PF_A32B32G32R32F:
                convertedData = ConvertFloatTo8(texture.PixelFormat, texture.Width, texture.Height, dataSpan, true);
                break;
            case EPixelFormat.PF_A16B16G16R16:
                convertedData = Convert16To8(texture.PixelFormat, texture.Width, texture.Height, dataSpan, true);
                break;
            case EPixelFormat.PF_G16:
                convertedData = Convert16To8(texture.PixelFormat, texture.Width, texture.Height, dataSpan);
                break;
            case EPixelFormat.PF_G16R16:
                convertedData = Convert16To8(texture.PixelFormat, texture.Width, texture.Height, dataSpan, true);
                break;
            default:
                throw new NotImplementedException("Unsupported pixel format: " + texture.PixelFormat);
        }

        var info = new SKImageInfo(texture.Width, texture.Height, skColorType, SKAlphaType.Premul);
        return InstallPixels(dataSpan, convertedData, info);
    }

    private static unsafe nint Convert16To8(EPixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> inp, bool flipOrder = false)
    {
        int channelCount = PixelFormatUtils.PixelFormats.First(x => x.UnrealFormat == pixelFormat).NumComponents;

        //(4 bytes per pixel for RGBA)
        MemoryUtils.NativeAlloc<byte>(width * height * 4, out var retPtr);

        fixed (byte* inpPtr = inp)
        {
            byte* outPtr = (byte*)retPtr;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = (y * width + x) * channelCount * sizeof(ushort);
                    for (int c = 0; c < channelCount; c++)
                    {
                        int channelIndex = flipOrder ? (3 - c) : c;
                        ushort value = *(ushort*)(inpPtr + pixelOffset + channelIndex * sizeof(ushort));
                        *outPtr = FColor.Requantize16to8(value);
                        outPtr += sizeof(byte);
                    }
                    FillMissingChannels(outPtr, channelCount);
                }
            }
        }
        return retPtr;
    }

    private static unsafe nint ConvertFloatTo8(EPixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> inp, bool flipOrder = false)
    {
        int channelCount = PixelFormatUtils.PixelFormats.First(x => x.UnrealFormat == pixelFormat).NumComponents;

        //(4 bytes per pixel for RGBA)
        MemoryUtils.NativeAlloc<byte>(width * height * 4, out var retPtr);

        fixed (byte* inpPtr = inp)
        {
            byte* outPtr = (byte*)retPtr;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = (y * width + x) * channelCount * sizeof(float);
                    for (int c = 0; c < channelCount; c++)
                    {
                        int channelIndex = flipOrder ? (3 - c) : c;
                        float value = *(float*)(inpPtr + pixelOffset + channelIndex * sizeof(float));
                        *outPtr = (byte)Math.Clamp(value * 255.0f, 0, byte.MaxValue);
                        outPtr += sizeof(byte);
                    }
                    FillMissingChannels(outPtr, channelCount);
                }
            }
        }
        return retPtr;
    }

    private static unsafe nint ConvertHalfTo8(EPixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> inp, bool flipOrder = false)
    {
        int channelCount = PixelFormatUtils.PixelFormats.First(x => x.UnrealFormat == pixelFormat).NumComponents;

        //(4 bytes per pixel for RGBA)
        MemoryUtils.NativeAlloc<byte>(width * height * 4, out var retPtr);

        fixed (byte* inpPtr = inp)
        {
            byte* outPtr = (byte*)retPtr;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = (y * width + x) * channelCount * sizeof(Half);
                    for (int c = 0; c < channelCount; c++)
                    {
                        int channelIndex = flipOrder ? (3 - c) : c;
                        float value = (float)*(Half*)(inpPtr + pixelOffset + channelIndex * sizeof(Half));
                        *outPtr = (byte)Math.Clamp(value * 255.0f, 0, byte.MaxValue);
                        outPtr += sizeof(byte);
                    }
                    FillMissingChannels(outPtr, channelCount);
                }
            }
        }
        return retPtr;
    }

    private static unsafe void FillMissingChannels(byte* outPtr, int channelCount)
    {
        for (int i = channelCount; i < 4; i++)
        {
            switch (i)
            {
                case 3:  //Alpha channel
                case 2 when channelCount == 2:  //Special case for RG format
                    *outPtr = byte.MaxValue; //fill with MaxValue
                    break;
                default: // Copy the last channel (grayscale value)
                    *outPtr = *(outPtr - sizeof(byte));
                    break;
            }
            outPtr += sizeof(byte);
        }
    }

    private static SKBitmap InstallPixels(ReadOnlySpan<byte> data, nint pixelsPtr, SKImageInfo info)
    {
        var bitmap = new SKBitmap();
        if (pixelsPtr == nint.Zero)
        {
            var pixelsSpan = MemoryUtils.NativeAlloc<byte>(data.Length, out pixelsPtr);
            data.CopyTo(pixelsSpan);
        }
        bitmap.InstallPixels(info, pixelsPtr, info.RowBytes, static (address, _) => MemoryUtils.NativeFree(address));
        return bitmap;
    }

    #endregion
}
