using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.Tests.Encryption;

public class AesEcbTests
{
    private static readonly FAesKey Key = new(Convert.FromHexString(
        "603deb1015ca71be2b73aef0857d7781" +
        "1f352c073b6108d72d9810a30914dff4"));
    private static readonly byte[] Plaintext = Convert.FromHexString(
        "6bc1bee22e409f96e93d7e117393172a" +
        "ae2d8a571e03ac9c9eb76fac45af8e51" +
        "30c81c46a35ce411e5fbc1191a0a52ef" +
        "f69f2445df4f9b17ad2b417be66c3710");
    private static readonly byte[] Ciphertext = Convert.FromHexString(
        "f3eed1bdb5d2a03c064b5a7e3db181f8" +
        "591ccb10d410ed26dc5ba74a31362870" +
        "b6ed21b99ca6f4f9f153e7b1beafed1d" +
        "23304b7a39f9f3ff067d8d8f9e24ecc7");

    [Fact]
    public void DecryptInPlaceDecryptsCompleteBlocks()
    {
        var actual = Ciphertext.ToArray();

        actual.DecryptInPlace(Key);

        Assert.Equal(Plaintext, actual);
    }

    [Fact]
    public void DecryptPreservesTheInputBuffer()
    {
        var input = Ciphertext.ToArray();

        var actual = input.Decrypt(Key);

        Assert.Equal(Plaintext, actual);
        Assert.Equal(Ciphertext, input);
    }

    [Fact]
    public void DecryptSupportsAnEncryptionUnitAtAnyArrayOffset()
    {
        var input = new byte[Ciphertext.Length + 7];
        Ciphertext.CopyTo(input, 3);

        var actual = input.Decrypt(3, Ciphertext.Length, Key);

        Assert.Equal(Plaintext, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 42)]
    [InlineData(15, 33)]
    [InlineData(16, 32)]
    [InlineData(31, 21)]
    [InlineData(63, 1)]
    public void DecryptSupportsRangesAtAnyByteOffset(int offset, int length)
    {
        var actual = Ciphertext.DecryptRange(offset, length, Key);

        Assert.True(Plaintext.AsSpan(offset, length).SequenceEqual(actual));
    }
}
