using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Dto.Custom.DBD;

public class DBDInstancedFoliageComponentDto : SceneComponentDto
{
    public DBDInstancedFoliageComponentDto(UDBDInstancedFoliageComponent foliage, ActorDto owner) : base(FTransform.Identity, foliage.Name, owner)
    {
        foreach (var (mesh, materials, transforms) in foliage.GetMeshes())
        {
            Children.Add(new InstancedStaticMeshComponentDto(mesh, transforms, materials, owner));
        }
    }
}
