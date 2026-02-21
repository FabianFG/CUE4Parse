namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoComponentCluster
{
    public string Name;
    public int ComponentClusterIndex;
    public FFastGeoStaticMeshComponent[] StaticMeshComponents;
    public FFastGeoInstancedStaticMeshComponent[] InstancedStaticMeshComponents;
    public FFastGeoSkinnedMeshComponent[] SkinnedMeshComponents;
    public FFastGeoInstancedSkinnedMeshComponent[] InstancedSkinnedMeshComponents;
    public FFastGeoProceduralISMComponent[] ProceduralISMComponents;

    public FFastGeoComponentCluster(FFastGeoArchive Ar)
    {
        Name = Ar.ReadFString();
        ComponentClusterIndex = Ar.Read<int>();
        StaticMeshComponents = Ar.ReadArray(() => new FFastGeoStaticMeshComponent(Ar));
        InstancedStaticMeshComponents = Ar.ReadArray(() => new FFastGeoInstancedStaticMeshComponent(Ar));
        SkinnedMeshComponents = Ar.ReadArray(() => new FFastGeoSkinnedMeshComponent(Ar));
        InstancedSkinnedMeshComponents = Ar.ReadArray(() => new FFastGeoInstancedSkinnedMeshComponent(Ar));
        ProceduralISMComponents = Ar.ReadArray(() => new FFastGeoProceduralISMComponent(Ar));
    }
}
