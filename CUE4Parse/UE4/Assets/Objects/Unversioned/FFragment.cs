namespace CUE4Parse.UE4.Assets.Objects.Unversioned;

public readonly struct FFragment(ushort packed)
{
    public const uint SkipMax = 127;
    public const uint ValueMax = 127;

    public const uint SkipNumMask = 0x007fu;
    public const uint HasZeroMask = 0x0080u;
    public const int ValueNumShift = 9;
    public const uint IsLastMask  = 0x0100u;

    public readonly byte SkipNum = (byte) (packed & SkipNumMask); // Number of properties to skip before values
    public readonly bool HasAnyZeroes = (packed & HasZeroMask) != 0;
    public readonly byte ValueNum = (byte) (packed >> ValueNumShift);  // Number of subsequent property values stored
    public readonly bool IsLast = (packed & IsLastMask) != 0; // Is this the last fragment of the header?
}
