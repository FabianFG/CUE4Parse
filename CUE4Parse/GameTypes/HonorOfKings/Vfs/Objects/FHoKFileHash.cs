using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CUE4Parse.GameTypes.HonorOfKings.Vfs.Objects;

public static class FHoKFileHash
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static ulong Compute(string text, bool addSlash)
    {
        ArgumentNullException.ThrowIfNull(text);

        int length = text.Length;
        Span<char> raw = length <= 256 ? stackalloc char[length+1] : new char[length+1];
        if (addSlash)
        {
            raw[0] = '/';
            text.AsSpan().ToLowerInvariant(raw[1..]);
            length++;
        }
        else
        {
            text.AsSpan().ToLowerInvariant(raw);
        }

        uint h1 = 0x5BD1E995;
        uint h2 = 0xAB9423A7;

        if (length == 0)
            return ((ulong)h2 << 32) | h1;

        int left = 0;
        int right = length - 1;

        while (left < length)
        {
            h1 = (h1 * 33u) ^ raw[left++];
            h2 = (h2 * 33u) ^ raw[right--];
        }

        return ((ulong)h2 << 32) | h1;
    }
}
