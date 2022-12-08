using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Nanite
{
    public class FNaniteData
    {
        public readonly FNaniteResources Resources;
        public readonly FStaticMeshSection[] MeshSections;

        public FNaniteData(FAssetArchive Ar)
        {
            Resources = new FNaniteResources(Ar);
            MeshSections = Ar.ReadArray(() => new FStaticMeshSection(Ar));
        }
    }
}
