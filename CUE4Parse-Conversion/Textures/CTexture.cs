using System;
using System.Runtime.InteropServices;

using CUE4Parse.UE4.Objects.Core.Math;

using OffiUtils;

using SkiaSharp;

namespace CUE4Parse_Conversion.Textures;

public enum PixelFormat
{
    PF_R8,
    PF_R16,
    PF_R32F,
    PF_RGB32F,
    PF_RGBA8,
    PF_RGBx8,
    PF_RGBA32F,
    PF_BGRA8,
    PF_MAX,
}

public class CTexture
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Data { get; }
    public PixelFormat PixelFormat { get; }

    public bool IsFloat => PixelFormat is PixelFormat.PF_R32F or PixelFormat.PF_RGB32F or PixelFormat.PF_RGBA32F;

    public CTexture(int width, int height, PixelFormat pixelFormat, byte[] data)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        Data = data;
    }

    public SKBitmap ToSkBitmap()
    {
        var dataSpan = new ReadOnlySpan<byte>(Data);
        var convertedData = nint.Zero;

        switch (PixelFormat)
        {
            case PixelFormat.PF_R32F:
                convertedData = Convert32FTo8(Width, Height, dataSpan, 1);
                break;
            case PixelFormat.PF_RGB32F:
                convertedData = Convert32FTo8(Width, Height, dataSpan, 3);
                break;
            case PixelFormat.PF_RGBA32F:
                convertedData = Convert32FTo8(Width, Height, dataSpan, 4);
                break;
            case PixelFormat.PF_R16:
                convertedData = ConvertRawR16DataToRGB888X(Width, Height, dataSpan);
                break;
        }

        var info = new SKImageInfo(Width, Height, GetSkColorType(PixelFormat), SKAlphaType.Premul);
        return InstallPixels(dataSpan, convertedData, info);
    }

    private static SKColorType GetSkColorType(PixelFormat pixelFormat) => pixelFormat switch
    {
        PixelFormat.PF_R8 => SKColorType.Gray8,
        PixelFormat.PF_R16 => SKColorType.Rgb888x,
        PixelFormat.PF_R32F => SKColorType.Rgba8888,
        PixelFormat.PF_RGB32F => SKColorType.Rgba8888,
        PixelFormat.PF_RGBx8 => SKColorType.Rgb888x,
        PixelFormat.PF_RGBA8 => SKColorType.Rgba8888,
        PixelFormat.PF_BGRA8 => SKColorType.Bgra8888,
        PixelFormat.PF_RGBA32F => SKColorType.Rgba8888,
        _ => throw new NotSupportedException($"Unsupported pixel format: {pixelFormat}")
    };

    //TODO: can be removed if span overload is valid
    private static unsafe byte[] ConvertRawR16DataToRGB888X(int width, int height, byte* inp)
    {
        int srcPitch = width * 2;
        // e.g. shadow maps
        var ret = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            var srcPtr = (ushort*) (inp + y * srcPitch);
            var destPtr = y * width * 4;
            for (int x = 0; x < width; x++)
            {
                var value16 = *srcPtr++;
                var value = FColor.Requantize16to8(value16);

                ret[destPtr++] = value;
                ret[destPtr++] = value;
                ret[destPtr++] = value;
                ret[destPtr++] = 255;
            }
        }

        return ret;
    }

    private static nint ConvertRawR16DataToRGB888X(int width, int height, ReadOnlySpan<byte> inp)
    {
        // e.g. shadow maps
        var inpU16 = MemoryMarshal.Cast<byte, ushort>(inp);
        var retSpan = MemoryUtils.NativeAlloc<FColor>(width * height, out var retPtr);
        for (int y = 0; y < height; y++)
        {
            var srcSpan = inpU16.Slice(y * width);
            var destOffset = y * width;
            for (int x = 0; x < width; x++)
            {
                var value16 = srcSpan[x];
                var value = FColor.Requantize16to8(value16);
                retSpan[destOffset + x] = new FColor(value, value, value, byte.MaxValue);
            }
        }

        return retPtr;
    }

    //TODO: can be removed if span overload is valid
    private static unsafe byte[] Convert32FTo8(int width, int height, byte* inp, int channelCount)
    {
        int totalSize = width * height * channelCount;
        byte[] ret = new byte[totalSize];

        fixed (byte* outPtr = ret)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte* pixelPtr = inp + (y * width + x) * channelCount * sizeof(float);

                    for (int c = 0; c < channelCount; c++)
                    {
                        float value = *((float*)(pixelPtr + c * sizeof(float)));
                        byte value8 = (byte)Math.Clamp(value * 255.0f, 0, 255);
                        int idx = (y * width + x) * channelCount + c;
                        outPtr[idx] = value8;
                    }
                }
            }
        }

        return ret;
    }

    private static nint Convert32FTo8(int width, int height, ReadOnlySpan<byte> inp, int channelCount)
    {
        int totalSize = width * height * channelCount;
        var retSpan = MemoryUtils.NativeAlloc<byte>(totalSize, out var retPtr);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixelOffset = (y * width + x) * channelCount * sizeof(float);

                for (int c = 0; c < channelCount; c++)
                {
                    float value = MemoryMarshal.Read<float>(inp.Slice(pixelOffset + c * sizeof(float)));
                    byte value8 = (byte)Math.Clamp(value * 255.0f, 0, 255);
                    int idx = (y * width + x) * channelCount + c;
                    retSpan[idx] = value8;
                }
            }
        }

        return retPtr;
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
}
