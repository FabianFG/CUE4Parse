using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

public abstract class TParticles<T>  where T : struct
{
    public int d { get; init; }
    public TVector<T>[] MX;

    public TParticles(int dimension)
    {
        d = dimension;
        MX = [];
    }

    public TParticles(TVector<T>[] mx, int dimension)
    {
        MX = mx;
        d = dimension;
    }

    public TParticles(FChaosArchive Ar, int dimension)
    {
        d = dimension;
        MX = [];
    }

    public virtual void Serialize(FChaosArchive Ar)
    {
        var bSerialize = Ar.ReadBoolean();
        MX = bSerialize ? Ar.ReadArray(() => new TVector<T>(Ar, d)) : []; // double serialized as single see SerializeReal vector.h
    }
}

public class FParticles : TParticles<float> // actually double TVector is going to be serialized as float so uh!
{
    public FParticles() : base(3) { }
    public FParticles(TVector<float>[] mx): base(mx, 3) { }
    public FParticles(FChaosArchive Ar) : base(Ar, 3) { }
}

public class FBVHParticles : FParticles, IChaosClass
{
    public TBoundingVolumeHierarchy<FParticles, int, float> MBVH;

    public FBVHParticles() { }

    public FBVHParticles(FChaosArchive Ar) : base(Ar) { }

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        MBVH = new TBoundingVolumeHierarchy<FParticles, int, float>(Ar, 3);
    }

    public static IChaosClass SerializationFactory(FChaosArchive Ar)
    {
        return new FBVHParticles(Ar);
    }
}
