using System;
using System.IO;
using VGAudio.Codecs.CriAdx;
using VGAudio.Containers.Adx;
using VGAudio.Containers.Wave;
using VGAudio.Formats;

namespace CUE4Parse.UE4.CriWare.Decoders.ADX;

public static class AdxDecoder
{
    public static byte[] ConvertAdxToWav(byte[] adxDataWithSubkey, ulong key)
    {
        if (adxDataWithSubkey == null || adxDataWithSubkey.Length <= sizeof(ushort))
            throw new ArgumentException("Invalid HCA data.");

        var (subKey, adxData) = adxDataWithSubkey.ExtractSubKey();

        if (subKey is 0)
            return []; // VGAudio decoder is outdated and only works in some cases, we only need to use it when audio is encrypted though as we can fallback to VgmStream instead

        if (subKey != 0 && key == 0) // Not sure if this is correct way to detect encryption on ADX
            throw new CriwareDecryptionException("CRIWARE audio is encrypted. Provide the correct decryption key in settings (numeric or hexadecimal format, up to 20 digits / 8 bytes).");

        if (subKey != 0)
        {
            key *= (((ulong) subKey << 16) | ((ushort) (~subKey + 2)));
        }

        var reader = new AdxReader
        {
            EncryptionKey = new CriAdxKey(key)
        };

        var audioWithConfig = reader.ReadWithConfig(adxData);
        AudioData audioData = audioWithConfig.Audio;

        using var stream = new MemoryStream();
        var writer = new WaveWriter();
        writer.WriteToStream(audioData, stream, null);

        return stream.ToArray();
    }
}
