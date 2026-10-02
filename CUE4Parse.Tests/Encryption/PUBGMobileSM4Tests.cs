using CUE4Parse.GameTypes.Tencent.PUBGMobile.Encryption.SM4;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.Tests.Encryption;

public class PUBGMobileSM4Tests
{
    private const uint TestKeyIndex = 0x00FFFFFE;
    private const string TestSalt = "dynamic-key-test-salt";

    [Fact]
    public void RegisteredDynamicKeyCanBeReadAndReset()
    {
        try
        {
            PUBGMobileSM4.RegisterDynamicKeySalt(TestKeyIndex, TestSalt);

            Assert.True(PUBGMobileSM4.TryGetDynamicKeySalt(TestKeyIndex, out var salt));
            Assert.Equal(TestSalt, salt);
            Assert.Contains(TestKeyIndex, PUBGMobileSM4.GetAvailableDynamicKeyIndices());
        }
        finally
        {
            PUBGMobileSM4.ResetDynamicKeySalt(TestKeyIndex);
        }

        Assert.False(PUBGMobileSM4.TryGetDynamicKeySalt(TestKeyIndex, out _));
    }

    [Fact]
    public void ResolverSuppliesAndCachesMissingDynamicKey()
    {
        try
        {
            PUBGMobileSM4.DynamicKeySaltResolver = index =>
            {
                Assert.Equal(TestKeyIndex, index);
                return TestSalt;
            };

            var encryptedBlock = new byte[16];
            var keyId = 0x01000000u | TestKeyIndex;
            var result = PUBGMobileSM4.Decrypt(encryptedBlock, 0, encryptedBlock.Length, "test.uasset",
                EPUBGMobileEncryptionMethod.SM4, keyId);

            Assert.Equal(16, result.Length);
            Assert.True(PUBGMobileSM4.TryGetDynamicKeySalt(TestKeyIndex, out var salt));
            Assert.Equal(TestSalt, salt);
        }
        finally
        {
            PUBGMobileSM4.DynamicKeySaltResolver = null;
            PUBGMobileSM4.ResetDynamicKeySalt(TestKeyIndex);
        }
    }

    [Fact]
    public void MissingDynamicKeyWithoutResolverThrows()
    {
        try
        {
            PUBGMobileSM4.DynamicKeySaltResolver = null;
            var keyId = 0x01000000u | TestKeyIndex;

            Assert.Throws<ParserException>(() => PUBGMobileSM4.Decrypt(new byte[16], 0, 16, "test.uasset",
                EPUBGMobileEncryptionMethod.SM4, keyId));
        }
        finally
        {
            PUBGMobileSM4.ResetDynamicKeySalt(TestKeyIndex);
        }
    }

    [Fact]
    public void ReplacementResolver_ReplacesOnlyTheRequestedDynamicSlot()
    {
        var originalResolver = PUBGMobileSM4.DynamicKeySaltReplacementResolver;
        try
        {
            PUBGMobileSM4.RegisterDynamicKeySalt(TestKeyIndex, "old-local-salt");
            uint? requestedSlot = null;
            PUBGMobileSM4.DynamicKeySaltReplacementResolver = slot =>
            {
                requestedSlot = slot;
                return "new-local-salt";
            };

            Assert.True(PUBGMobileSM4.TryReplaceDynamicKeySalt(TestKeyIndex));
            Assert.Equal(TestKeyIndex, requestedSlot);
            Assert.True(PUBGMobileSM4.TryGetDynamicKeySalt(TestKeyIndex, out var salt));
            Assert.Equal("new-local-salt", salt);
        }
        finally
        {
            PUBGMobileSM4.DynamicKeySaltReplacementResolver = originalResolver;
            PUBGMobileSM4.ResetDynamicKeySalt(TestKeyIndex);
        }
    }

    [Fact]
    public void ReplacementResolver_WithoutAValueLeavesTheExistingSlotUntouched()
    {
        var originalResolver = PUBGMobileSM4.DynamicKeySaltReplacementResolver;
        try
        {
            PUBGMobileSM4.RegisterDynamicKeySalt(TestKeyIndex, "old-local-salt");
            PUBGMobileSM4.DynamicKeySaltReplacementResolver = _ => null;

            Assert.False(PUBGMobileSM4.TryReplaceDynamicKeySalt(TestKeyIndex));
            Assert.True(PUBGMobileSM4.TryGetDynamicKeySalt(TestKeyIndex, out var salt));
            Assert.Equal("old-local-salt", salt);
        }
        finally
        {
            PUBGMobileSM4.DynamicKeySaltReplacementResolver = originalResolver;
            PUBGMobileSM4.ResetDynamicKeySalt(TestKeyIndex);
        }
    }
}
