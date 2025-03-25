using SkiaSharp;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

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

    private readonly PixelFormat PixelFormat;
    public byte[] Data { get; }

    public bool IsFloat => PixelFormat == PixelFormat.PF_R32F || PixelFormat == PixelFormat.PF_RGB32F || PixelFormat == PixelFormat.PF_RGBA32F;

    public CTexture(int width, int height, PixelFormat pixelFormat, byte[] data)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        Data = data;
    }

    public SKBitmap ToSkBitmap()
    {
        var convertedData = Data;

        switch (PixelFormat)
        {
            case PixelFormat.PF_R32F:
                unsafe
                {
                    fixed (byte* d = Data) //Convert 32bit float to 8bit
                        convertedData = Convert32FTo8(Width, Height, d, 1);
                }
                break;
            case PixelFormat.PF_RGB32F:
                unsafe
                {
                    fixed (byte* d = Data) //Convert 32bit float to 8bit
                        convertedData = Convert32FTo8(Width, Height, d, 3);
                }
                break;
            case PixelFormat.PF_RGBA32F:
                unsafe
                {
                    fixed (byte* d = Data) //Convert 32bit float to 8bit
                        convertedData = Convert32FTo8(Width, Height, d, 4);
                }
                break;
            case PixelFormat.PF_R16:
                unsafe
                {
                    fixed (byte* d = Data) //Convert R16 to RGB888X
                        convertedData = ConvertRawR16DataToRGB888X(Width, Height, d);
                }
                break;
        }

        var info = new SKImageInfo(Width, Height, GetSkColorType(PixelFormat), SKAlphaType.Premul);
        return InstallPixels(convertedData, info);
    }

    private static SKColorType GetSkColorType(PixelFormat pixelFormat)
    {
        return pixelFormat switch
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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


    private static SKBitmap InstallPixels(byte[] data, SKImageInfo info)
    {
        var bitmap = new SKBitmap();
        unsafe
        {
            var pixelsPtr = NativeMemory.Alloc((nuint) data.Length);
            fixed (byte* p = data)
            {
                Unsafe.CopyBlockUnaligned(pixelsPtr, p, (uint) data.Length);
            }

            bitmap.InstallPixels(info, new IntPtr(pixelsPtr), info.RowBytes, (address, _) => NativeMemory.Free(address.ToPointer()));
        }
        return bitmap;
    }
}
