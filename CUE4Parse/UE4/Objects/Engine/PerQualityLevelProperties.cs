using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine;

public class FPerQualityLevelProperty<T> : IUStruct where T : struct
{
    public readonly bool bCooked;
    public readonly T Default;
    public readonly Dictionary<int, T> PerQuality = [];

    public FPerQualityLevelProperty() { }

    public FPerQualityLevelProperty(FArchive Ar)
    {
        bCooked = Ar.ReadBoolean();
        Default = Ar.Read<T>();
        PerQuality = Ar.ReadMap(Ar.Read<int>, Ar.Read<T>);
    }
}

public class FPerQualityLevelInt : FPerQualityLevelProperty<int>
{
    public FPerQualityLevelInt() { }
    public FPerQualityLevelInt(FArchive Ar) : base(Ar) { }  
}


public class FPerQualityLevelFloat : FPerQualityLevelProperty<float>
{
    public FPerQualityLevelFloat() { }
    public FPerQualityLevelFloat(FArchive Ar) : base(Ar) { } 
}
