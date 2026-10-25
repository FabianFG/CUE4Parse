using System.Text;

namespace CUE4Parse.UE4.Wwise;

public static class WwiseFnv
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    public static uint GetHash(string name)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name.ToLowerInvariant());
        return ComputeHash(nameBytes);
    }

    private static uint ComputeHash(byte[] nameBytes)
    {
        uint hash = FnvOffsetBasis;
        foreach (byte b in nameBytes)
        {
            hash *= FnvPrime;
            hash ^= b;
            hash &= 0xFFFFFFFF; // Clamp to 32-bits
        }

        return hash;
    }
}
