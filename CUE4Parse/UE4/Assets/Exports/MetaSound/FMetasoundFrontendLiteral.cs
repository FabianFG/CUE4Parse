using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MetaSound;

[StructFallback]
public class FMetasoundFrontendLiteral
{
    public EMetasoundFrontendLiteralType Type;
    public int AsNumDefault;
    public bool[] AsBoolean;
    public int[] AsInteger;
    public float[] AsFloat;
    public string[] AsString;
    public FPackageIndex[] AsUObject;
    
    public FMetasoundFrontendLiteral(FStructFallback fallback)
    {
        Type =  fallback.GetOrDefault<EMetasoundFrontendLiteralType>(nameof(Type));
        AsNumDefault = fallback.GetOrDefault<int>(nameof(AsNumDefault));
        AsBoolean = fallback.GetOrDefault<bool[]>(nameof(AsBoolean), []);
        AsInteger = fallback.GetOrDefault<int[]>(nameof(AsInteger), []);
        AsFloat = fallback.GetOrDefault<float[]>(nameof(AsFloat), []);
        AsString = fallback.GetOrDefault<string[]>(nameof(AsString), []);
        AsUObject = fallback.GetOrDefault<FPackageIndex[]>(nameof(AsUObject), []);
    }
}

public enum EMetasoundFrontendLiteralType : byte
{
    None, //< A value of None expresses that an object being constructed with a literal should be default constructed.
    Boolean,
    Integer,
    Float,
    String,
    UObject,

    NoneArray, //< A NoneArray expresses the number of objects to be default constructed.
    BooleanArray,
    IntegerArray,
    FloatArray,
    StringArray,
    UObjectArray,

    Invalid
}