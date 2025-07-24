using System;
using System.Collections.Generic;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public abstract class TEveryPlatformProperty<T>(FAssetArchive Ar, Func<T> getter) : IUStruct
{
    public T DefaultValue = getter();
    public Dictionary<FName, T> PlatformOverrides = Ar.ReadMap(Ar.ReadFName, getter);
}

public class FEveryPlatformFloat(FAssetArchive Ar) : TEveryPlatformProperty<float>(Ar, Ar.Read<float>);
public class FEveryPlatformBool(FAssetArchive Ar) : TEveryPlatformProperty<bool>(Ar, Ar.ReadBoolean);
public class FEveryPlatformInt(FAssetArchive Ar) : TEveryPlatformProperty<int>(Ar, Ar.Read<int>);
