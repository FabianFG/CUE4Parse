using System.Buffers.Binary;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.Textures.Crunch;

internal static unsafe class CrunchDecoder
{
    private const int MinHeaderSize = 0x4B;
    private const ushort CrnMagic = 0x4878; // "Hx", stored big-endian

    static CrunchDecoder() => TextureNativeLibrary.Prepare("crunch.dll");

    [DllImport("crunch", EntryPoint = "crnd_unpack_begin")]
    private static extern void* UnpackBegin(byte* data, uint dataSize);

    [DllImport("crunch", EntryPoint = "crnd_unpack_level_segmented")]
    private static extern bool UnpackLevelSegmented(void* context, byte* source, uint sourceSize,
        void** destination, uint destinationSize, uint rowPitch, uint levelIndex);

    [DllImport("crunch", EntryPoint = "crnd_unpack_end")]
    private static extern bool UnpackEnd(void* context);

    public static byte[] DecompressMip(byte[] source, int sizeX, int sizeY, int sizeZ, FPixelFormatInfo formatInfo)
    {
        var blocksX = sizeX.DivideAndRoundUp(formatInfo.BlockSizeX);
        var blocksY = sizeY.DivideAndRoundUp(formatInfo.BlockSizeY);
        var blocksZ = sizeZ.DivideAndRoundUp(formatInfo.BlockSizeZ);
        var rowPitch = checked(blocksX * formatInfo.BlockBytes);
        var outputSize = checked(rowPitch * blocksY * blocksZ);

        if (source.Length < MinHeaderSize || BinaryPrimitives.ReadUInt16BigEndian(source) != CrnMagic)
        {
            // PUBG Mobile keeps the _crunched format name on small mips that it stores uncompressed
            if (source.Length == outputSize)
                return source;

            throw new ParserException("Invalid Crunch texture mip header");
        }

        var declaredSize = BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(6));
        var levelCount = BinaryPrimitives.ReadUInt16BigEndian(source.AsSpan(0x10));
        var levelOffset = BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(0x47));
        if (declaredSize > source.Length || levelCount == 0 || levelOffset < MinHeaderSize || levelOffset >= declaredSize)
            throw new ParserException("Invalid Crunch texture mip level range");

        var output = new byte[outputSize];

        using var context = new CrunchContext(source, 0, checked((int) declaredSize));
        if (!context.TryDecompressSegment(source, checked((int) levelOffset), declaredSize - levelOffset, output, (uint) rowPitch, 0))
            throw new ParserException("Failed to decompress Crunch texture mip");

        return output;
    }

    public sealed class CrunchContext : IDisposable
    {
        private void* _handle;

        public CrunchContext(byte[] source, int offset, int count)
        {
            if (offset < 0 || count <= 0 || offset > source.Length - count)
                throw new ArgumentOutOfRangeException(nameof(count));

            fixed (byte* sourcePtr = source)
            {
                _handle = UnpackBegin(sourcePtr + offset, (uint) count);
            }

            if (_handle == null)
                throw new ParserException("Failed to initialize Crunch decompression");
        }

        public bool TryDecompressSegment(byte[] source, int offset, uint count, byte[] destination, uint rowPitch, uint levelIndex)
        {
            ObjectDisposedException.ThrowIf(_handle == null, this);
            if (offset < 0 || count == 0 || count > int.MaxValue || offset > source.Length - (int) count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (destination.Length == 0)
                throw new ArgumentException("Crunch destination cannot be empty", nameof(destination));

            fixed (byte* sourcePtr = source)
            fixed (byte* destinationPtr = destination)
            {
                void* output = destinationPtr;
                return UnpackLevelSegmented(_handle, sourcePtr + offset, count, &output, (uint) destination.Length, rowPitch, levelIndex);
            }
        }

        public void Dispose()
        {
            if (_handle == null)
                return;

            UnpackEnd(_handle);
            _handle = null;
        }
    }
}
