using System;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class ALandscapeProxy : AActor
{
    public int ComponentSizeQuads { get; private set; }
    public int SubsectionSizeQuads { get; private set; }
    public int NumSubsections { get; private set; }
    public FPackageIndex[] LandscapeComponents = [];
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
        LandscapeComponents = GetOrDefault<FPackageIndex[]>(nameof(LandscapeComponents), Array.Empty<FPackageIndex>());
        LandscapeSectionOffset = GetOrDefault<int>(nameof(LandscapeSectionOffset));
        LandscapeMaterial = GetOrDefault<FPackageIndex>(nameof(LandscapeMaterial), new FPackageIndex());
        SplineComponent = GetOrDefault<FPackageIndex>(nameof(SplineComponent), new FPackageIndex());
        LandscapeGuid = GetOrDefault<FGuid>(nameof(LandscapeGuid));
    }
}

public class ALandscape: ALandscapeProxy { }
public class ALandscapeStreamingProxy: ALandscapeProxy { }
