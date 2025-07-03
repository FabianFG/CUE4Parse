using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class ALandscapeProxy : APartitionActor
{
    public int ComponentSizeQuads { get; private set; }
    public int SubsectionSizeQuads { get; private set; }
    public int NumSubsections { get; private set; }
    public FPackageIndex[] LandscapeComponents = [];
    public FPackageIndex[] NaniteComponents = [];
    public int LandscapeSectionOffset { get; private set; }
    public FPackageIndex LandscapeMaterial { get; private set; }
    public FPackageIndex SplineComponent { get; private set; }
    public FGuid LandscapeGuid { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ComponentSizeQuads = GetOrDefault<int>(nameof(ComponentSizeQuads));
        SubsectionSizeQuads = GetOrDefault<int>(nameof(SubsectionSizeQuads));
        NumSubsections = GetOrDefault<int>(nameof(NumSubsections));
        LandscapeComponents = GetOrDefault<FPackageIndex[]>(nameof(LandscapeComponents), []);
        LandscapeSectionOffset = GetOrDefault<int>(nameof(LandscapeSectionOffset));
        LandscapeMaterial = GetOrDefault(nameof(LandscapeMaterial), new FPackageIndex());
        SplineComponent = GetOrDefault(nameof(SplineComponent), new FPackageIndex());
        if (Ar.Game >= EGame.GAME_UE5_3)
            NaniteComponents = GetOrDefault<FPackageIndex[]>(nameof(NaniteComponents), []);
        else
        {
            var naniteComponent = GetOrDefault<FPackageIndex?>("NaniteComponent", null);
            NaniteComponents = naniteComponent != null ? [naniteComponent] : [];
        }
        LandscapeGuid = GetOrDefault<FGuid>(nameof(LandscapeGuid));
    }
}

public class ALandscape: ALandscapeProxy { }
public class ALandscapeStreamingProxy: ALandscapeProxy { }
