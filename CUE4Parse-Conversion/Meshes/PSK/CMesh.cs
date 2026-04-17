using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.PSK;

public abstract class CMesh<TLod>(FBox box, FSphere sphere) : IDisposable where TLod : CMeshLod
{
    public readonly FBox BoundingBox = box;
    public readonly FSphere BoundingSphere = sphere;
    public readonly List<TLod> LODs = [];

    protected CMesh(FBoxSphereBounds bounds) : this(new FBox(bounds.Origin - bounds.BoxExtent, bounds.Origin + bounds.BoxExtent), new FSphere(0f, 0f, 0f, bounds.SphereRadius / 2))
    {

    }

    public virtual void FinalizeMesh()
    {
        foreach (var levelOfDetail in LODs)
        {
            levelOfDetail.BuildNormals();
        }
    }

    public virtual void Dispose()
    {
        foreach (var lod in LODs)
            lod.Dispose();

        LODs.Clear();
    }
}
