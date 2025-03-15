using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.SOT.Assets.Export;


public class UInstancedCoverageMeshComponent: UStaticMeshComponent
{
    public override FPackageIndex GetStaticMesh() {
        if (StaticMesh != null)
            return StaticMesh;
        var mesh = new FPackageIndex();
        var current = this;
        while (true)
        {
            if (current is null) break;
            mesh = current.GetOrDefault("CoveredMesh", new FPackageIndex());
            if (!mesh.IsNull || current.Template == null)
                break;
            current = current.Template.Load<UInstancedCoverageMeshComponent>();
        }
        if (mesh.IsNull)
        {
            mesh = base.GetStaticMesh();
        }
        StaticMesh = mesh;
        return mesh;
    }
}