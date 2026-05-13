using System;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class FDwordBitReader(uint[] buffer, uint offset = 0) : IDisposable
{
    private uint[] _buffer = buffer;
    private uint _offset = offset;

    public uint GetBits(uint numBits)
    {
        if (numBits > 32)
            throw new InvalidOperationException("NumBits > 32");
        
        if (_offset + numBits > _buffer.Length * 32)
            throw new InvalidOperationException("_offset + numBits > _buffer.Length * 32");

        if (numBits == 0)
            return 0;
            
        var baseIndex = _offset >> 5;
        var bitOffset = _offset & 31;

        _offset += numBits;

        if (bitOffset + numBits > 32)
        {
            var bitMaskLow = (1 << (int)(32 - bitOffset)) - 1;
            var bitMaskHigh = (1 << (int)(numBits + bitOffset - 32)) - 1;
            var bitOffsetLow = bitOffset;
            var bitOffsetHigh = 32 - bitOffset;

            var low = (_buffer[baseIndex + 0] >> (int)bitOffsetLow) & bitMaskLow;
            var high = (_buffer[baseIndex + 1] & bitMaskHigh) << (int)bitOffsetHigh;

            return (uint)(low | high);
        }

        var bitMask = (1ul << (int)numBits) - 1;
        return (uint)((_buffer[baseIndex] >> (int)bitOffset) & bitMask);
    }

    public void Dispose()
    {
        _buffer = [];
    }
}