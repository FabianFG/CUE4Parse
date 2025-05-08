namespace CUE4Parse.GameTypes.SOD2.Assets;

public static class SOD2Properties
{
    public static bool GetMapPropertyTypes(string? name, out string? keyType, out string? valueType)
    {
        switch (name)
        {
            case "ThreadsByName":
            case "Conversations":
            case "Scenes":
            case "BuffDefs":
            case "CastingMap":
            case "CriticalMissionMap":
            case "ObjectivesMap":
            case "PhaseEventMap":
            case "ArcMissionEventIndices":
            case "RequirementsMap":
            case "CookedComponentInstancingData":
            case "CollectionEntryCache":
            case "Characters":
                keyType = "NameProperty";
                valueType = "StructProperty";
                break;
            case "Assets":
                keyType = "StrProperty";
                valueType = "StructProperty";
                break;
            case "AllHumanDefinitions":
                keyType = "NameProperty";
                valueType = "ObjectProperty";
                break;
            case "SortedDefinitionsByFace":
                keyType = "StructProperty";
                valueType = "StructProperty";
                break;
            case "PrimitiveComponentToSampleResourceId":
            case "FacilitySlotToSampleResourceId":
                keyType = "ObjectProperty";
                valueType = "StructProperty";
                break;
            case "SearchableMaterials":
            case "InteractableMaterials":
                keyType = "ObjectProperty";
                valueType = "ObjectProperty";
                break;
            case "ComponentMap":
                keyType = "UInt32Property";
                valueType = "ObjectProperty";
                break;
            case "GameModeDescriptors":
            case "ZombieTextNameStructs":
            case "ZombieTextNameAlternatesStructs":
            case "ZombieBloodPlagueQualifierStructs":
                keyType = "EnumProperty";
                valueType = "TextProperty";
                break;
            default:
                keyType = null;
                valueType = null;
                return false;
        }
        return true;
    }
}
