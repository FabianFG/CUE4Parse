using System.Buffers.Binary;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private static void Decrypt(Span<byte> payload, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        switch (spec.CipherKind)
        {
            case CipherKind.ChaCha:
                ProSpiChaCha(payload, trailer, spec);
                return;
            case CipherKind.ProSpiCustomPermuted:
                ProSpiCustomPermutedXor(payload, trailer, spec);
                return;
            case CipherKind.ProSpiCustomPermutedAlt:
                ProSpiCustomPermutedAltXor(payload, trailer, spec);
                return;
            case CipherKind.ProSpiSalsaRorPremixed:
                ProSpiSalsaRorPremixedXor(payload, trailer, spec);
                return;
            case CipherKind.ProSpiCustomPermutedPremixed:
                ProSpiCustomPermutedPremixedXor(payload, trailer, spec);
                return;
            case CipherKind.ProSpiSalsaRor:
                ProSpiSalsaRorXor(payload, trailer, spec);
                return;
            default:
                Log.Warning($"Unsupported cipher kind: {spec.CipherKind}");
                break;
        }
    }

    private static uint ReadTrailerWord(ReadOnlySpan<byte> trailer, int wordOffset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(trailer.Slice(wordOffset * sizeof(uint), sizeof(uint)));
}
