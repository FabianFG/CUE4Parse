using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Engine;

[StructFallback]
public struct FComponentKey: IUStruct 
{
    public UClass OwnerClass;
    public FName SCSVariableName;
    public FGuid AssociatedGuid;
    
    public FComponentKey(FStructFallback fallback)
    {
        OwnerClass = fallback.GetOrDefault<UClass>("OwnerClass");
        SCSVariableName = fallback.GetOrDefault<FName>("SCSVariableName");
        AssociatedGuid = fallback.GetOrDefault<FGuid>("AssociatedGuid");
    }
}

[StructFallback]
public struct FComponentOverrideRecord : IUStruct 
{
    public FPackageIndex ComponentClass;
    public FPackageIndex? ComponentTemplate; // UActorComponent*
    public FComponentKey ComponentKey;
    // public FBlueprintCookedComponentInstancingData CookedComponentInstancingData;
    
    public FComponentOverrideRecord(FStructFallback fallback) 
    {
        ComponentClass = fallback.GetOrDefault<FPackageIndex>(nameof(ComponentClass));
        ComponentTemplate = fallback.GetOrDefault<FPackageIndex>(nameof(ComponentTemplate));
        ComponentKey = fallback.GetOrDefault<FComponentKey>(nameof(ComponentKey));
    }

}

// https://github.com/EpicGames/UnrealEngine/blob/c830445187784f1269f43b56f095493a27d5a636/Engine/Source/Runtime/Engine/Classes/Engine/InheritableComponentHandler.h#L92
// Stores data to override (in children classes) components (created by SCS) from parent classes
public class UInheritableComponentHandler: UObject 
{
    /** All component records */
    public FComponentOverrideRecord[] GetRecords() => GetOrDefault<FComponentOverrideRecord[]>("Records", []);

    public FComponentKey FindKey(string variableName) 
    {
        foreach (var record in GetRecords()) 
        {
            if (record.ComponentKey.SCSVariableName == variableName || (record.ComponentTemplate != null && record.ComponentTemplate.Name == variableName)) 
            {
                return record.ComponentKey;
            }    
        }
        return default;
    }
    
    public FPackageIndex? FindTemplate(string variableName) 
    {
        foreach (var record in GetRecords()) 
        {
            if (record.ComponentKey.SCSVariableName == variableName || (record.ComponentTemplate != null && record.ComponentTemplate.Name == variableName)) 
            {
                return record.ComponentTemplate;
            }    
        }
        return null;
    }
}