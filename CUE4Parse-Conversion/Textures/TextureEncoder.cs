using System;
using System.IO;
using System.Text;
using SkiaSharp;

namespace CUE4Parse_Conversion.Textures;

public static class TextureEncoder
{
    public static byte[] Encode(this CTexture bitmap, ETextureFormat format, out string ext)
    {
        //always export float data as HDR
        if (bitmap.IsFloat)
        {
            ext = "hdr";
            return EncodeHdr(bitmap);
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

    private static byte[] EncodeHdr(CTexture bitmap)
    {
        var stream = new MemoryStream();

        // Radiance HDR Header
        string header = "#?RADIANCE\n# Written by CUE4Parse\nFORMAT=32-bit_rle_rgbe\n";
        stream.Write(Encoding.ASCII.GetBytes(header));
        stream.Write("\n"u8);
        string resolutionLine = $"-Y {bitmap.Height} +X {bitmap.Width}\n";
        stream.Write(Encoding.ASCII.GetBytes(resolutionLine));

        unsafe
        {
            fixed (byte* dataPointer = bitmap.Data)
            {
                byte[] scanlineBuffer = new byte[4 * bitmap.Width];  // 4 channels per pixel

                for (int y = 0; y < bitmap.Height; y++)
                {
                    // Get pointer to the current row of RGBA floats (correct offset calculation)
                    var rowPointer = (float*)(dataPointer + y * bitmap.Width * 4 * sizeof(float));

                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        // Read RGBA float values
                        float r = *rowPointer++;
                        float g = *rowPointer++;
                        float b = *rowPointer++;
                        float a = *rowPointer++; // Alpha (ignored in HDR)

                        // Find the maximum color component
                        float maxValue = Math.Max(r, Math.Max(g, b));

                        byte rByte, gByte, bByte, exponentByte;

                        if (maxValue < 1e-32f) // If the color is essentially black
                        {
                            rByte = gByte = bByte = exponentByte = 0;
                        }
                        else
                        {
                            // Calculate the exponent
                            int exponent = 0;
                            float tempValue = maxValue;
                            while (tempValue < 0.5f)
                            {
                                tempValue *= 2.0f;
                                exponent--;
                            }
                            while (tempValue >= 1.0f)
                            {
                                tempValue /= 2.0f;
                                exponent++;
                            }

                            // Scale RGB components based on the exponent
                            float scaleFactor = 256.0f / MathF.Pow(2, exponent);
                            rByte = (byte)(r * scaleFactor);
                            gByte = (byte)(g * scaleFactor);
                            bByte = (byte)(b * scaleFactor);

                            // Store the exponent with bias 128
                            exponentByte = (byte)(exponent + 128);
                        }

                        // Store the data in the scanline buffer (RGBAE format)
                        scanlineBuffer[x] = rByte;
                        scanlineBuffer[x + bitmap.Width] = gByte;
                        scanlineBuffer[x + 2 * bitmap.Width] = bByte;
                        scanlineBuffer[x + 3 * bitmap.Width] = exponentByte;
                    }

                    // Write the scanline with RLE
                    WriteScanlineRLE(stream, scanlineBuffer, bitmap.Width);
                }
            }
        }

        return stream.ToArray();
    }

    private static void WriteScanlineRLE(MemoryStream stream, byte[] scanlineBuffer, int scanlineWidth)
    {
        // Write the scanline header (scanline type 2 and width)
        stream.WriteByte(2); // Scanline type (uncompressed)
        stream.WriteByte(2); // Scanline type (uncompressed)
        stream.WriteByte((byte)(scanlineWidth >> 8)); // High byte of the width
        stream.WriteByte((byte)(scanlineWidth & 0xFF)); // Low byte of the width

        // Process each channel (RGBA) separately
        for (int channel = 0; channel < 4; channel++)
        {
            WriteChannelRLE(stream, scanlineBuffer, scanlineWidth, channel);
        }
    }

    private static void WriteChannelRLE(MemoryStream stream, byte[] scanlineBuffer, int scanlineWidth, int channel)
    {
        int current = 0;

        while (current < scanlineWidth)
        {
            // Try to find a run of identical values for the current channel
            int runLength = 1;

            // Check for run-length (values that are the same)
            while (current + runLength < scanlineWidth && runLength < 127 &&
                   scanlineBuffer[current + channel * scanlineWidth] == scanlineBuffer[current + runLength + channel * scanlineWidth])
            {
                runLength++;
            }

            // If we found a run of 4 or more identical values, encode the run using RLE
            if (runLength >= 4)
            {
                stream.WriteByte((byte)(128 + runLength)); // Start of RLE, 128 + runLength
                stream.WriteByte(scanlineBuffer[current + channel * scanlineWidth]); // Value to repeat
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
                           scanlineBuffer[current + nonRunLength + channel * scanlineWidth] == scanlineBuffer[current + nonRunLength + nextRun + channel * scanlineWidth])
                    {
                        nextRun++;
                    }

                    // Break early if a run is detected
                    if (nextRun >= 4)
                        break;

                    nonRunLength++;
                }

                // Write the non-run length values
                stream.WriteByte((byte)nonRunLength); // Write non-run length (1-128)
                for (int i = 0; i < nonRunLength; i++)
                {
                    stream.WriteByte(scanlineBuffer[current + i + channel * scanlineWidth]);
                }
                current += nonRunLength;
            }
        }
    }

}
