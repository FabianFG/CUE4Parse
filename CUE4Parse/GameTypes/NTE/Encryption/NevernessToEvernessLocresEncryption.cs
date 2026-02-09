using System;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.NTE.Encryption;

public static class FNTEFTextLocalizationResource
{
    public static readonly FAesKey NTELocresAesKey = new FAesKey("0x396d4330686f704b4e6a5377694364684e56375974435765754476484c513238");

    public static FTextLocalizationResourceString[] ReadLocResStringArray(FArchive Ar)
    {
        var version = Ar.Read<int>();
        var isEncrypted = version switch
        {
            >= 10100 => Ar.ReadBoolean(),
            >= 10000 => true,
            _ => false
        };

        var localizedStringArrayOffset = Ar.Read<long>();
        if (localizedStringArrayOffset != -1) // INDEX_NONE
        {
            var currentFileOffset = Ar.Position;
            Ar.Position = localizedStringArrayOffset;
            var localizedStringArray = Ar.ReadArray(() => new FTextLocalizationResourceString(ReadEncryptedString(Ar, isEncrypted), Ar.Read<int>()));
            Ar.Position = currentFileOffset;
            return localizedStringArray;
        }

        return [];
    }

    private static string ReadEncryptedString(FArchive Ar, bool bEncrypted)
    {
        if (!bEncrypted) return Ar.ReadFString();
        var encryptedBytes = Convert.FromBase64String(Ar.ReadFString().Replace('-', '+').Replace('_', '/'));
        var decrypted = Encoding.UTF8.GetString(encryptedBytes.Decrypt(NTELocresAesKey)).Split("HottaLocresSplit")[0];
        return decrypted;
    }

}
