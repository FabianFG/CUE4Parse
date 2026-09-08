namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption;

internal interface ICustomizableCryptoAlgorithm
{
    public int KeySize { get; }
    public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key);
}
