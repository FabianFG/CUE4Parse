namespace CUE4Parse.UE4.Objects.RigVM;

public enum ERigVMPinDirection : byte
{
    Input, // A const input value
    Output, // A mutable output value
    IO, // A mutable input and output value
    Visible, // A const value that cannot be connected to
    Hidden, // A mutable hidden value (used for interal state)
    Invalid // The max value for this enum - used for guarding.
}
