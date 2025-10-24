using CUE4Parse.UE4.Assets.Objects;

namespace CUE4Parse.GameTypes.DaysGone.Assets;

public static class DaysGoneProperties
{
    public static (string?, string?) GetMapPropertyTypes(string? name)
    {
        string? keyType;
        string? valueType;
        switch (name)
        {
            case "ScalarMap":
                keyType = "NameProperty";
                valueType = "IntProperty";
                break;
            case "SoundCueMap":
            case "SoundCueAssetMap":
            case "PlayerSoundCueMap":
            case "FriendSoundCueMap":
            case "ParticleMap":
                keyType = "NameProperty";
                valueType = "ObjectProperty";
                break;
            default:
                keyType = null;
                valueType = null;
                break;
        }
        return (keyType, valueType);
    }

    public static FPropertyTagData? GetArrayStructType(string? name, int elementSize)
    {
        var structType = name switch
        {
            "ParameterIds" => "Guid",
            "ReferenceFunctionIdCache" => "Guid",

            "Keys" when elementSize == 27 => "RichCurveKey",

            "SwipeStart" => "Vector2D",
            "InputCurvePoints" => "Vector2D",
            "StickyScorePositions" => "Vector2D",
            "SectorsToRender" => "Vector2D",
            "MapPerimeterPoly" => "Vector2D",

            "Colors" => "LinearColor",
            "SubtitleColors" => "LinearColor",
            "BackgroundColors" => "LinearColor",
            "SubtitleColor" => "LinearColor",
            "IndexedColors" => "LinearColor",
            "PrimaryColor" => "LinearColor",
            "SecondaryColor" => "LinearColor",
            "RankColor" => "LinearColor",
            "DebugPathColors" => "LinearColor",
            "InteractButtonIconColorCorrespondingArray" => "LinearColor",
            "InteractButtonTextureColorCorrespondingArray" => "LinearColor",

            "initialCompRot" => "Rotator",

            "VertexData" when elementSize == 8 => "Vector2D",
            "VertexData" => "Vector",

            "Points" when elementSize == 12 => "Vector",
            "Points_BP" => "Vector",
            "Positions" => "Vector",
            "Normals" => "Vector",
            "LocalNormals" => "Vector",
            "LocalPositions" => "Vector",
            "TransformedNormals" => "Vector",
            "TransformedPositions" => "Vector",
            "GeneratedLocations" => "Vector",
            "TrackingGameplayLocations" => "Vector",
            "StaticNavigableGeometry" => "Vector",
            "LocationsForConversationToAvoid" => "Vector",
            "FacingSpeedRanges" => "Vector",
            "s_FoamClothScale" => "Vector",
            "s_MeshScales" => "Vector",
            "s_MeshMassOffset" => "Vector",
            "VectorLocationsForTunnels_Temp" => "Vector",
            "BreadcrumbLocations" => "Vector",
            "ObjectiveLocations" => "Vector",
            "ValidEndLocations" => "Vector",
            "s_DummyInitPos" => "Vector",
            "Position_10_4C15053445A03427D18C5D96DBE7C392" => "Vector",
            "ExtraSoundLocations" => "Vector",
            "EndLocations" => "Vector",
            "DebugBikeTrailRenderCoords" => "Vector",
            "TempRagerBoxExtents" => "Vector",
            "SpawnPoints" => "Vector",
            "Bike Position" => "Vector",

            "IncludeMeshes" => "StringAssetReference",
            "ExcludeMeshes" => "StringAssetReference",
            "IncludePhysicalMaterials" => "StringAssetReference",
            "ExcludePhysicalMaterials" => "StringAssetReference",

            _ => null,
        };
        return structType is not null ? new FPropertyTagData(structType) : null;
    }


}
