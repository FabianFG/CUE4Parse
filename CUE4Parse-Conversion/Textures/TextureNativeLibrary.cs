using System.Reflection;
using System.Resources;
using System.Security.Cryptography;

namespace CUE4Parse_Conversion.Textures;

internal static class TextureNativeLibrary
{
    public static void Prepare(string dllName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"CUE4Parse_Conversion.Resources.{dllName}");
        if (stream == null)
            throw new MissingManifestResourceException($"Couldn't find {dllName} in Embedded Resources");

        var embeddedData = new byte[(int) stream.Length];
        _ = stream.Read(embeddedData, 0, embeddedData.Length);

        var fileOk = false;
        using (var sha1 = SHA1.Create())
        {
            var embeddedHash = sha1.ComputeHash(embeddedData);
            if (File.Exists(dllName))
                fileOk = embeddedHash.AsSpan().SequenceEqual(sha1.ComputeHash(File.ReadAllBytes(dllName)));
        }

        if (!fileOk)
            File.WriteAllBytes(dllName, embeddedData);
    }
}
