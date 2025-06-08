using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
                convertedDataPtr = ConvertToRGBE<Half>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, x => (float)x);
                break;

            case EPixelFormat.PF_R32_FLOAT:
            case EPixelFormat.PF_R32G32B32F:
                convertedDataPtr = ConvertToRGBE<float>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, x => x);
                break;

            case EPixelFormat.PF_A32B32G32R32F:
                convertedDataPtr = ConvertToRGBE<float>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, x => x, flipOrder: true);
                break;

            default:
                throw new NotImplementedException("Unsupported pixel format: " + texture.PixelFormat);
        }

        if (convertedDataPtr == nint.Zero)
            throw new Exception("Failed to convert texture data to RGBE format.");

        using var stream = new MemoryStream();

        WriteHdrHeader(stream, texture.Width, texture.Height);

        for (int y = 0; y < texture.Height; y++)
            WriteScanlineRLE(stream, convertedDataPtr, texture.Width, y * texture.Width * 4);

        MemoryUtils.NativeFree(convertedDataPtr);
        return stream.ToArray();
    }

    private static void WriteHdrHeader(Stream stream, int width, int height)
    {
        stream.Write("#?RADIANCE\n# Written by CUE4Parse\nFORMAT=32-bit_rle_rgbe\n\n"u8);
        stream.Write(Encoding.ASCII.GetBytes($"-Y {height} +X {width}\n"));
    }

    // TODO cant cast to float from T so have to use Func<T, float>
    private static unsafe nint ConvertToRGBE<T>(EPixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> inp, Func<T, float> toFloat, bool flipOrder = false) where T : unmanaged
    {
        int channelCount = PixelFormatUtils.PixelFormats.First(x => x.UnrealFormat == pixelFormat).NumComponents;

        MemoryUtils.NativeAlloc<byte>(width * height * 4, out var retPtr);

        fixed (byte* inpPtr = inp)
        {
            byte* outPtr = (byte*)retPtr;
            int elementSize = sizeof(T);

            for (int i = 0; i < width * height; i++)
            {
                int pixelOffset = i * channelCount * elementSize;

                float r = 0, g = 0, b = 0;

                if (channelCount == 1)
                    r = g = b = toFloat(*(T*)(inpPtr + pixelOffset));
                else
                {
                    for (int c = 0; c < channelCount; c++)
                    {
                        int channelIndex = flipOrder ? (channelCount - 1 - c) : c;
                        T value = *(T*)(inpPtr + pixelOffset + channelIndex * elementSize);
                        float channelValue = toFloat(value);

                        if (c == 0)
                            r = channelValue;
                        else if (c == 1)
                            g = channelValue;
                        else if (c == 2)
                            b = channelValue;
                    }
                }

                float maxValue = Math.Max(r, Math.Max(g, b));

                byte rByte, gByte, bByte, exponentByte;
                if (maxValue < 1e-32f)
                    rByte = gByte = bByte = exponentByte = 0;
                else
                {
                    int exponent = (int)Math.Floor(Math.Log2(maxValue)) + 1;
                    float scaleFactor = 256.0f / MathF.Pow(2, exponent);

                    rByte = (byte)(r * scaleFactor);
                    gByte = (byte)(g * scaleFactor);
                    bByte = (byte)(b * scaleFactor);
                    exponentByte = (byte)(exponent + 128);
                }

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
            WriteChannelRLE(stream, dataPtr, scanlineWidth, rowOffset, channel);
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
                    runLength++;

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
                        while (current + nonRunLength + nextRun < scanlineWidth && nextRun < 127 && *(pixelPtr + (nonRunLength * 4)) == *(pixelPtr + ((nonRunLength + nextRun) * 4)))
                            nextRun++;

                        // Break early if a run is detected
                        if (nextRun >= 4)
                            break;

                        nonRunLength++;
                    }

                    // Write the non-run length values
                    stream.WriteByte((byte)nonRunLength);
                    for (int i = 0; i < nonRunLength; i++)
                        stream.WriteByte(*(pixelPtr + (i * 4)));

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
            case EPixelFormat.PF_G8:
                skColorType = SKColorType.Gray8;
                break;
            case EPixelFormat.PF_FloatRGB:
            case EPixelFormat.PF_FloatRGBA:
                convertedData = ConvertTo8<Half>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, ConvertHalfTo8);
                break;
            case EPixelFormat.PF_R32_FLOAT:
            case EPixelFormat.PF_R32G32B32F:
                convertedData = ConvertTo8<float>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, ConvertFloatTo8);
                break;
            case EPixelFormat.PF_A32B32G32R32F:
                convertedData = ConvertTo8<float>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, ConvertFloatTo8, true);
                break;
            case EPixelFormat.PF_A16B16G16R16:
                convertedData = ConvertTo8<ushort>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, Convert16To8, true);
                break;
            case EPixelFormat.PF_G16:
                convertedData = ConvertTo8<ushort>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, Convert16To8);
                break;
            case EPixelFormat.PF_G16R16:
                convertedData = ConvertTo8<ushort>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, Convert16To8, true);
                break;
            case EPixelFormat.PF_G16R16F:
                convertedData = ConvertTo8<Half>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, ConvertHalfTo8, true);
                break;
            case EPixelFormat.PF_G32R32F:
                convertedData = ConvertTo8<float>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, ConvertFloatTo8, true);
                break;
            case EPixelFormat.PF_R16F:
                convertedData = ConvertTo8<Half>(texture.PixelFormat, texture.Width, texture.Height, dataSpan, ConvertHalfTo8);
                break;
            default:
                throw new NotImplementedException("Unsupported pixel format: " + texture.PixelFormat);
        }

        var info = new SKImageInfo(texture.Width, texture.Height, skColorType, SKAlphaType.Unpremul);
        return InstallPixels(dataSpan, convertedData, info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Convert16To8(ushort value)
    {
        return FColor.Requantize16to8(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ConvertFloatTo8(float value)
    {
        return (byte)Math.Clamp(value * 255.0f, 0, byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ConvertHalfTo8(Half value)
    {
        return (byte)Math.Clamp((float)value * 255.0f, 0, byte.MaxValue);
    }

    private static unsafe nint ConvertTo8<T>(EPixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> inp, Func<T, byte> conversionFunc, bool flipOrder = false)
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
                    int pixelOffset = (y * width + x) * channelCount * sizeof(T);
                    for (int c = 0; c < channelCount; c++)
                    {
                        int channelIndex = flipOrder ? (channelCount - 1 - c) : c;
                        T value = *(T*)(inpPtr + pixelOffset + channelIndex * sizeof(T));
                        *outPtr = conversionFunc(value);
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
