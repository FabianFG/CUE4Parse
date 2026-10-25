namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption;

internal interface IConfigurableCryptoAlgorithm
{
    public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key);
    public byte[] DecryptChunked(byte[] ciphertext, ReadOnlyMemory<byte> key) => Decrypt(ciphertext, key);
}
