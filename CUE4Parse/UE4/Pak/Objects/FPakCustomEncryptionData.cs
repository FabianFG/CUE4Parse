namespace CUE4Parse.UE4.Pak.Objects;

// Context for custom encryption of a pak entry
public readonly record struct FPakCustomEncryptionData(FPakEntry Entry, long Position);
