namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption;

internal interface ICustomizableCryptoAlgorithm
{
    public int MaximumKeySize { get; }
    public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key);
}
