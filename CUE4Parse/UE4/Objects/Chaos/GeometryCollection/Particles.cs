using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class TParticles<T> : ISerializationFactory where T : struct
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
        // Serialize(Ar);
    }

    public virtual ISerializationFactory Serialize(FChaosArchive Ar)
    {
        var bSerialize = Ar.ReadBoolean();
        MX = bSerialize ? Ar.ReadArray(() => new TVector<T>(Ar, d)) : []; // double serialized as single see SerializeReal vector.h
                                                                                // basically all TVector are serialized as singles??
        return this;
    }

    public virtual ISerializationFactory SerializationFactory(FChaosArchive Ar)
    {
        return new TParticles<T>(d);
    }
}

public class FParticles : TParticles<float> // actually double TVector is going to be serialized as float so uh!
{
    public FParticles() : base(3) { }
    // public FParticles(TVector<FReal>[] mx): base(mx, 3) { }

    public FParticles(FChaosArchive Ar) : base(Ar, 3) { }
}


public class FBVHParticles : FParticles
{

    // TBoundingVolumeHierarchy<FParticles, TArray<int32>, FReal, 3>* MBVH;
    public FBVHParticles() { }

    // public FBVHParticles(TVector<FReal>[] mx) : base(mx) { }

    public FBVHParticles(FChaosArchive Ar) : base(Ar) { }

    public override ISerializationFactory Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);

        // <FParticles, int, FReal (serialized as float)>
        TBoundingVolumeHierarchy<FParticles, int, float> MBVH = new TBoundingVolumeHierarchy<FParticles, int, float>(Ar, 3);

        return this;
    }

    public override ISerializationFactory SerializationFactory(FChaosArchive Ar)
    {
        return this;
    }
}
