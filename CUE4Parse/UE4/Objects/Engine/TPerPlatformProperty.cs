using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine;

public abstract class TPerPlatformProperty<T> : IUStruct where T : notnull
{
    public readonly bool bCooked;
    public T Default;
    public Dictionary<FName, T>? PerPlatform;
    public T Value => Default;

    public TPerPlatformProperty() { }

    public TPerPlatformProperty(FAssetArchive Ar, Func<T> getValue)
    {
        bCooked = Ar.ReadBoolean();
        Default = getValue();
        if (!bCooked && (Ar.Game is >= GAME_UE5_8 || Ar.IsFilterEditorOnly))
        {
            PerPlatform = Ar.ReadMap(Ar.ReadFName, getValue);
        }
    }
}

public class FPerPlatformBool : TPerPlatformProperty<bool>
{
    public FPerPlatformBool() { }
    public FPerPlatformBool(FAssetArchive Ar) : base(Ar, Ar.ReadBoolean) { }
}

public class FPerPlatformFloat : TPerPlatformProperty<float>
{
    public FPerPlatformFloat() { }
    public FPerPlatformFloat(float value) { Default = value; }
    public FPerPlatformFloat(FAssetArchive Ar) : base(Ar, Ar.Read<float>) { }
}

public class FPerPlatformInt : TPerPlatformProperty<int>
{
    public FPerPlatformInt() { }
    public FPerPlatformInt(FAssetArchive Ar) : base(Ar, Ar.Read<int>) { }
}

public class FPerPlatformFrameRate : TPerPlatformProperty<FFrameRate>
{
    public FPerPlatformFrameRate() { }
    public FPerPlatformFrameRate(FAssetArchive Ar) : base(Ar, Ar.Read<FFrameRate>) { }
}

public class FPerPlatformFString : TPerPlatformProperty<string>
{
    public FPerPlatformFString() { }
    public FPerPlatformFString(FAssetArchive Ar) : base(Ar, Ar.ReadFString) { }
}

public enum ERigLogicFloatingPointType : byte
{
    Float,
    HalfFloat,
    Auto
};

public class FPerPlatformERigLogicFloatingPointType : TPerPlatformProperty<ERigLogicFloatingPointType>
{
    public FPerPlatformERigLogicFloatingPointType() { }
    public FPerPlatformERigLogicFloatingPointType(FAssetArchive Ar) : base(Ar, () => (ERigLogicFloatingPointType) Ar.Read<int>()) { }
}

public enum ERigLogicCalculationType : byte
{
    Scalar,
    SSE,
    AVX,
    NEON,
    AnyVector
};

public class FPerPlatformERigLogicCalculationType : TPerPlatformProperty<ERigLogicCalculationType>
{
    public FPerPlatformERigLogicCalculationType() { }
    public FPerPlatformERigLogicCalculationType(FAssetArchive Ar) : base(Ar, () => (ERigLogicCalculationType) Ar.Read<int>()) { }
}

//FFreezablePerPlatformInt
//FFreezablePerPlatformFloat
