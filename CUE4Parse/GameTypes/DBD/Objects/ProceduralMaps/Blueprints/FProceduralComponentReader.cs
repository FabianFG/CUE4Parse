using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;

public static class FProceduralComponentReader
{
    public static IReadOnlyList<(UActorSpawner Spawner, FTransform Transform)> ReadSpawners(UBlueprintGeneratedClass blueprint) => ReadComponents<UActorSpawner>(blueprint);

    public static Dictionary<string, FTransform> ReadPreviewTransforms(UBlueprintGeneratedClass blueprint)
    {
        var transforms = new Dictionary<string, FTransform>(StringComparer.Ordinal);
        if (blueprint.SuperStruct?.Name != "Hatch")
            return transforms;

        foreach (var (name, component, _) in ReadNamedComponents<USkeletalMeshComponent>(blueprint))
        {
            if (name != "Hatch")
                continue;

            // Hatch's closed idle moves the root 120 cm along Y and turns it 180 degrees from the reference pose
            // Apply that fixed correction because we use non animated mesh
            transforms[name] = new FTransform(new FRotator(0, 180, 0), new FVector(0, 120, 0), FVector.OneVector) * component.GetRelativeTransform();
        }
        return transforms;
    }

    public static IReadOnlyList<(T Component, FTransform Transform)> ReadComponents<T>(UBlueprintGeneratedClass blueprint) where T : USceneComponent
        => [.. ReadNamedComponents<T>(blueprint).Select(entry => (entry.Component, entry.Transform))];

    public static IReadOnlyList<(string Name, T Component, FTransform Transform)> ReadNamedComponents<T>(UBlueprintGeneratedClass blueprint) where T : USceneComponent
    {
        var classes = new List<UBlueprintGeneratedClass>();
        var visitedClasses = new HashSet<UBlueprintGeneratedClass>();
        for (var currentClass = blueprint; currentClass != null && visitedClasses.Add(currentClass); currentClass = currentClass.SuperStruct?.Load() as UBlueprintGeneratedClass)
        {
            classes.Add(currentClass);
        }

        var overridesByGuid = new Dictionary<FGuid, FPackageIndex>();
        var overridesByName = new Dictionary<(string Class, string Variable), FPackageIndex>();
        foreach (var currentClass in classes)
        {
            if (currentClass.InheritableComponentHandler?.Load<UInheritableComponentHandler>() is not { } handler)
                continue;

            foreach (var record in handler.Records)
            {
                if (record.ComponentTemplate is not { IsNull: false } template)
                    continue;

                if (record.ComponentKey.AssociatedGuid.IsValid())
                {
                    overridesByGuid.TryAdd(record.ComponentKey.AssociatedGuid, template);
                }

                if (record.ComponentKey.OwnerClass is { } owner)
                {
                    overridesByName.TryAdd((owner.Name, record.ComponentKey.SCSVariableName.Text), template);
                }
            }
        }

        var components = new List<(string, T, FTransform)>();
        var hasRoot = false;
        var transformsByName = new Dictionary<string, FTransform>(StringComparer.Ordinal);
        var visitedNodes = new HashSet<FPackageIndex>();
        for (var classIndex = classes.Count - 1; classIndex >= 0; classIndex--)
        {
            var currentClass = classes[classIndex];
            if (currentClass.SimpleConstructionScript?.Load<USimpleConstructionScript>() is not { } script)
                continue;

            foreach (var root in script.RootNodes)
            {
                Visit(root, currentClass.Name, FTransform.Identity);
            }
        }

        return components;

        void Visit(FPackageIndex? nodePtr, string ownerClass, FTransform parentTransform)
        {
            if (nodePtr is not { IsNull: false } || !visitedNodes.Add(nodePtr) || nodePtr.Load<USCS_Node>() is not { } node)
                return;

            var template = node.ComponentTemplate;
            if (overridesByGuid.TryGetValue(node.VariableGuid, out var overrideTemplate) || overridesByName.TryGetValue((ownerClass, node.InternalVariableName.Text), out overrideTemplate))
                template = overrideTemplate;

            if (node.GetOrDefault<FName>("ParentComponentOrVariableName") is { IsNone: false } parentName && transformsByName.TryGetValue(parentName.Text, out var inheritedParent))
                parentTransform = inheritedParent;

            var transform = parentTransform;
            if (template?.Load<USceneComponent>() is { } component)
            {
                // The spawn transform replaces the SCS root's authored transform
                transform = hasRoot ? component.GetRelativeTransform() * parentTransform : FTransform.Identity;
                hasRoot = true;
                transformsByName[node.InternalVariableName.Text] = transform;
                if (component is T match)
                {
                    components.Add((node.InternalVariableName.Text, match, transform));
                }
            }

            foreach (var child in node.ChildNodes)
            {
                Visit(child, ownerClass, transform);
            }
        }
    }
}
