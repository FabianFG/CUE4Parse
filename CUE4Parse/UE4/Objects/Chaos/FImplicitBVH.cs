using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

using FObjects = FImplicitBVHObject[];
using FVec3f = TIntVector3<float>;

public class FImplicitBVHObject
{
    private FImplicitObject Geometry;
    private FVec3f X;
    private TIntVector4<float> R;
    private int RootObjectIndex;
    
    public FImplicitBVHObject(FChaosArchive Ar)
    {
        Geometry = Ar.SerializePtr(new FImplicitObject());

        X = Ar.Read<FVec3f>();
        R = Ar.Read<TIntVector4<float>>();
        RootObjectIndex = Ar.Read<int>();
    }
}

public class TBVHNode<T> where T: struct
{
    public TVector<T> MMin;
    public TVector<T> MMax;
    public int MAxis;
    public int[] MChildren;
    public int LeafIndex;

    public TBVHNode(FChaosArchive Ar, int d)
    {
        LeafIndex = Ar.Read<int>();
        MAxis = Ar.Read<int>();
        MChildren = Ar.ReadArray<int>();
        var aMMax = new TVector<float>(Ar, d);
        var aMMin = new TVector<float>(Ar, d);
    }
}

// T = FReal (double)
// d= 3
public class TBoundingVolumeHierarchy<OBJECT_ARRAY, LEAF_TYPE, T> where T: struct where LEAF_TYPE : struct
{
    public int[] GlobalObjects;

    public Dictionary<int, TAABB<T>> MWorldSpaceBoxes;

    public int MMaxLevels;
    public TBVHNode<T>[] Elements;
    public LEAF_TYPE[][] Leafs;
    
    public TBoundingVolumeHierarchy(FChaosArchive Ar, int d)
    {
        GlobalObjects = Ar.ReadArray<int>();
        MWorldSpaceBoxes = TBox<T>.SerializeAsAABBs(Ar, d);

        MMaxLevels = Ar.Read<int>();

        Elements = Ar.ReadArray(() => new TBVHNode<T>(Ar, d));
        Leafs = Ar.ReadArray(Ar.ReadArray<LEAF_TYPE>);
    }
}

public class FImplicitBVH
{
    public FObjects Objects;
    public int[] NodeObjectIndicesArray;

    public FImplicitBVH(FChaosArchive Ar)
    {
        Objects = Ar.ReadArray(() => new FImplicitBVHObject(Ar));
        var BVH = new TBoundingVolumeHierarchy<int[], int, FReal>(Ar, 3);
    }
}
