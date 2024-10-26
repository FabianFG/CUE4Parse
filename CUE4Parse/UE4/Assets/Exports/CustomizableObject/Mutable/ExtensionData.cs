using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class ExtensionData
{
    public short Index;
    public EOrigin Origin;
    
    public ExtensionData(FAssetArchive Ar)
    {
        Index = Ar.Read<short>();
        Origin = Ar.Read<EOrigin>();
    }
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