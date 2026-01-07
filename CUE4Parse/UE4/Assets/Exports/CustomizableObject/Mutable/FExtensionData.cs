using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

[StructLayout(LayoutKind.Sequential)]
public struct FExtensionData
{
    public short Index;
    public EOrigin Origin;
}

public enum EOrigin : byte
{
    //! An invalid value used to indicate that this ExtensionData hasn't been initialized
    Invalid,

    //! This ExtensionData is a compile-time constant that's always loaded into memory
    ConstantAlwaysLoaded,

    //! This ExtensionData is a compile-time constant that's streamed in from disk when needed
    ConstantStreamed,

    //! This ExtensionData was generated at runtime
    Runtime
}