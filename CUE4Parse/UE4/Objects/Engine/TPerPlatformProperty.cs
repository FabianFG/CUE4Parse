using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine;

public abstract class TPerPlatformProperty<T> : IUStruct where T : notnull
{
    public bool bCooked;
    public T Default;
    public Dictionary<FName, T>? PerPlatform;
    public T Value => Default;

    public TPerPlatformProperty() { }

    public TPerPlatformProperty(FAssetArchive Ar, Func<T> getValue)
    {
        bCooked = Ar.ReadBoolean();
        Default = getValue();
        if (Ar.IsFilterEditorOnly && !bCooked)
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
