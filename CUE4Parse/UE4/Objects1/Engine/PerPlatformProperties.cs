global using FPerPlatformInt = CUE4Parse.UE4.Objects.Engine.TPerPlatformProperty<int>;
global using FPerPlatformBool = CUE4Parse.UE4.Objects.Engine.TPerPlatformProperty<bool>;
global using FPerPlatformFloat = CUE4Parse.UE4.Objects.Engine.TPerPlatformProperty<float>;
global using FPerPlatformFrameRate = CUE4Parse.UE4.Objects.Engine.TPerPlatformProperty<CUE4Parse.UE4.Objects.Core.Misc.FFrameRate>;
using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine;

public struct TPerPlatformProperty<T> : IUStruct
{
    public readonly bool bCooked;
    public readonly T? Default;

    public TPerPlatformProperty() { }

    public TPerPlatformProperty(FArchive Ar)
    {
        bCooked = Ar.ReadBoolean();
        Default = Ar.Read<T>();
    }

    public TPerPlatformProperty(FArchive Ar, Func<T> readFunc)
    {
        bCooked = Ar.ReadBoolean();
        Default = readFunc();
    }
}
