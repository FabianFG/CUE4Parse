using System;
using System.IO;
using VGAudio.Containers.Adx;
using VGAudio.Containers.Wave;

namespace CUE4Parse.UE4.CriWare.Decoders.ADX;

public static class AdxDecoder
{
    private static readonly byte[][] ADX_SIGNATURES =
    {
        new byte[] { 0x03, 0x12, 0x04, 0x01, 0x00, 0x00 },
        new byte[] { 0x03, 0x12, 0x04, 0x02, 0x00, 0x00 }
    };

    private static readonly byte[] CRI_COPYRIGHT = { 0x28, 0x63, 0x29, 0x43, 0x52, 0x49 }; // "(c)CRI"

    public static byte[] ConvertAdxToWav(byte[] rawData, ulong key)
    {
        try
        {
            Console.WriteLine($"[ADX Decode] Original data length: {rawData?.Length}");

            byte[] extractedAdx = ExtractAdxFromMemory(rawData);
            if (extractedAdx == null || extractedAdx.Length == 0)
            {
                Console.WriteLine("[ADX Decode] Error: Unable to extract ADX from file");
                throw new InvalidDataException("No valid ADX data found");
            }
            Console.WriteLine($"[ADX Decode] Extracted ADX data length: {extractedAdx.Length}");
            
            // Debug output: first 64 bytes
            for (int i = 0; i < Math.Min(64, extractedAdx.Length); i++)
            {
                if (i % 16 == 0) Console.Write("\n");
                Console.Write($"{extractedAdx[i]:X2} ");
            }
            Console.WriteLine();

            return DecodeAdxToWav(extractedAdx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADX Decode Error] {ex.Message}");
            throw;
        }
    }

    private static byte[] ExtractAdxFromMemory(byte[] data)
    {
        if (data == null || data.Length < 0x20)
            return null;

        for (int i = 0; i <= data.Length - 0x20; i++)
        {
            // Look for ADX signature: 0x80 0x00
            if (data[i] == 0x80 && data[i + 1] == 0x00)
            {
                bool hasValidSignature = false;
                
                // Check the 6-byte sequence at offset 4
                foreach (var signature in ADX_SIGNATURES)
                {
                    bool match = true;
                    for (int j = 0; j < 6; j++)
                    {
                        if (i + 4 + j >= data.Length || data[i + 4 + j] != signature[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        hasValidSignature = true;
                        Console.WriteLine($"[ADX Extract] Found valid ADX signature at position {i}");
                        break;
                    }
                }

                if (!hasValidSignature)
                    continue;

                // Search for copyright information "(c)CRI"
                bool hasCopyright = false;
                int copyrightSearchStart = i + 0x14;
                int copyrightSearchEnd = Math.Min(i + 0x1000, data.Length - 6);

                for (int j = copyrightSearchStart; j <= copyrightSearchEnd; j++)
                {
                    bool match = true;
                    for (int k = 0; k < 6; k++)
                    {
                        if (j + k >= data.Length || data[j + k] != CRI_COPYRIGHT[k])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        hasCopyright = true;
                        Console.WriteLine($"[ADX Extract] Found copyright information at position {j}");
                        break;
                    }
                }

                if (!hasCopyright)
                {
                    Console.WriteLine($"[ADX Extract] Found ADX signature at position {i} but no copyright information");
                    continue;
                }

                Console.WriteLine($"[ADX Extract] Found valid ADX, removing first {i} bytes");

                // Extract ADX data from current position to end
                int adxLength = data.Length - i;
                byte[] adxData = new byte[adxLength];
                Buffer.BlockCopy(data, i, adxData, 0, adxLength);

                return adxData;
            }
        }
        
        Console.WriteLine("[ADX Extract] No valid ADX data found");
        return null;
    }

    private static byte[] DecodeAdxToWav(byte[] adxData)
    {
        try
        {
            Console.WriteLine("[ADX Decode] Using VGAudio to decode...");

            var reader = new AdxReader();
            var audioWithConfig = reader.ReadWithConfig(adxData);

            if (audioWithConfig?.Audio == null)
                throw new InvalidOperationException("VGAudio returned null audio data");

            using var stream = new MemoryStream();
            var writer = new WaveWriter();
            writer.WriteToStream(audioWithConfig.Audio, stream, null);

            byte[] wavData = stream.ToArray();
            Console.WriteLine($"[ADX Decode] Success, WAV size: {wavData.Length}");
            return wavData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADX Decode] Decoding failed: {ex.Message}");
            throw;
        }
    }
}
