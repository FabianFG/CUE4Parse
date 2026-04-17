using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;

public class ULandscapeSplinesComponent : UPrimitiveComponent
{
    public FPackageIndex?[] Segments = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Segments = GetOrDefault(nameof(Segments), Segments);
    }

    public override IEnumerable<UObject> GetExportableReferences()
    {
        foreach (var ptr in Segments)
        {
            if (ptr?.TryLoad<ULandscapeSplineSegment>(out var segment) == true)
                foreach (var meshPtr in segment.LocalMeshComponents)
                {
                    if (meshPtr?.TryLoad<USplineMeshComponent>(out var splineMesh) == true)
                        yield return splineMesh;
                }
        }

        foreach (var obj in base.GetExportableReferences())
            yield return obj;
    }
}
