using System.Text;

namespace CUE4Parse.UE4.Wwise;

public static class WwiseFnv
{
    public static uint GetHash(string name)
    {
        return GetHashLower(name.ToLowerInvariant());
    }

    public static uint GetHashLower(string lowerName)
    {
        var nameBytes = Encoding.UTF8.GetBytes(lowerName);
        return ComputeHash(nameBytes);
    }

    private static uint ComputeHash(byte[] nameBytes)
    {
        uint hash = 2166136261; // FNV offset basis

        foreach (byte b in nameBytes)
        {
            hash *= 16777619;   // FNV prime
            hash ^= b;
            hash &= 0xFFFFFFFF; // Clamp to 32-bits
        }

        return hash;
    }
}
