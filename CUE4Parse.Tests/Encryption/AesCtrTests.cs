using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.Tests.Encryption;

public class AesCtrTests
{
    private const uint InitialBlockIndex = 0xfcfdfeff;
    private static readonly FAesKey Key = new(Convert.FromHexString(
        "603deb1015ca71be2b73aef0857d7781" +
        "1f352c073b6108d72d9810a30914dff4"));
    private static readonly byte[] InitializationVector = Convert.FromHexString(
        "f0f1f2f3f4f5f6f7f8f9fafb");
    private static readonly byte[] Plaintext = Convert.FromHexString(
        "6bc1bee22e409f96e93d7e117393172a" +
        "ae2d8a571e03ac9c9eb76fac45af8e51" +
        "30c81c46a35ce411e5fbc1191a0a52ef" +
        "f69f2445df4f9b17ad2b417be66c3710");
    private static readonly byte[] Ciphertext = Convert.FromHexString(
        "601ec313775789a5b7a7f504bbf3d228" +
        "f443e3ca4d62b59aca84e990cacaf5c5" +
        "2b0930daa23de94ce87017ba2d84988d" +
        "dfc9c58db67aada613c2dd08457941a6");

    [Fact]
    public void CryptCtrDecryptsAlignedData()
    {
        var actual = Ciphertext.ToArray();

        actual.AsSpan().CryptCtrInPlace(Key, InitializationVector, InitialBlockIndex);

        Assert.Equal(Plaintext, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 42)]
    [InlineData(15, 33)]
    [InlineData(16, 32)]
    [InlineData(31, 21)]
    [InlineData(63, 1)]
    public void CryptCtrDecryptsDataAtAnyByteOffset(int offset, int length)
    {
        var actual = Ciphertext.AsSpan(offset, length).ToArray();
        var blockIndex = checked(InitialBlockIndex + (uint) (offset / Aes.ALIGN));
        var blockByteOffset = offset % Aes.ALIGN;

        actual.AsSpan().CryptCtrInPlace(Key, InitializationVector, blockIndex, blockByteOffset);

        Assert.True(Plaintext.AsSpan(offset, length).SequenceEqual(actual));
    }
}
