using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

public sealed class BlueprintDto : SceneDto
{
    public BlueprintDto(UBlueprintGeneratedClass blueprint, CancellationToken ct = default) : base(blueprint)
    {
        ct.ThrowIfCancellationRequested();
        var actor = new BlueprintActorDto(blueprint);
        if (actor.RootComponent != null)
        {
            Actors.Add(actor);
        }
    }
}

public class BlueprintActorDto : ActorDto
{
    private const int MaxChildActorDepth = 8;

    public BlueprintActorDto(UBlueprintGeneratedClass blueprint) : this(blueprint, [], 0)
    {

    }

    private BlueprintActorDto(UBlueprintGeneratedClass blueprint, HashSet<string> ancestry, int depth) : base(blueprint)
    {
        var classKey = blueprint.GetPathName();
        var nativeByName = new Dictionary<string, SceneComponentDto>(StringComparer.Ordinal);
        var byTemplatePtr = new Dictionary<FPackageIndex, SceneComponentDto>();
        var scsByVariable = new Dictionary<(string OwnerClass, string Variable), SceneComponentDto>();
        var scsByName = new Dictionary<string, SceneComponentDto>(StringComparer.Ordinal);
        var visitedNodes = new HashSet<FPackageIndex>();
        var overridesByGuid = new Dictionary<FGuid, FPackageIndex>();
        var overridesByKey = new Dictionary<(string OwnerClass, string Variable), FPackageIndex>();
        SceneComponentDto? root = null;

        var chain = CollectClassChain();
        BuildNativeComponents();
        BuildOverrideTable();
        ExecuteConstructionScripts();

        RootComponent = root;

        List<UBlueprintGeneratedClass> CollectClassChain()
        {
            var result = new List<UBlueprintGeneratedClass>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            var current = blueprint;
            while (current != null && seen.Add(current.GetPathName()))
            {
                result.Add(current);
                current = current.SuperStruct is { IsNull: false } super && super.TryLoad<UBlueprintGeneratedClass>(out var parent) ? parent : null;
            }

            return result;
        }

        void BuildNativeComponents()
        {
            // collect default subobjects of every class default object in the chain, the most-derived export of a given name wins
            var subobjects = new Dictionary<string, FPackageIndex>(StringComparer.Ordinal);
            UObject? mostDerivedCdo = null;
            foreach (var bp in chain)
            {
                var cdoPtr = bp.ClassDefaultObject;
                if (cdoPtr is not { IsNull: false } || cdoPtr.ResolvedObject is not { ExportIndex: >= 0 } cdo) continue;

                mostDerivedCdo ??= cdoPtr.Load();

                var package = cdo.Package;
                for (var i = 0; i < package.ExportMapLength; i++)
                {
                    var ptr = new FPackageIndex(package, i + 1);
                    var resolved = package.ResolvePackageIndex(ptr);
                    if (resolved?.Outer is not { } outer || outer.ExportIndex != cdo.ExportIndex || !ReferenceEquals(outer.Package, package)) continue;

                    subobjects.TryAdd(resolved.Name.Text, ptr);
                }
            }

            var created = new List<SceneComponentDto>();
            foreach (var (name, ptr) in subobjects)
            {
                if (ComponentDto.Create(ptr, this) is not SceneComponentDto component) continue;

                created.Add(component);
                nativeByName[name] = component;
                byTemplatePtr[ptr] = component;
            }

            // the attach parent pointer may target an ancestor package's export of the same subobject, so resolve it by name
            var parented = new HashSet<SceneComponentDto>();
            foreach (var component in created)
            {
                if (component._attachParent is { IsNull: false } parentPtr && nativeByName.TryGetValue(parentPtr.Name, out var parent) && parent != component)
                {
                    parent.Children.Add(component);
                    parented.Add(component);
                }
            }

            if (mostDerivedCdo?.GetOrDefault<FPackageIndex?>("RootComponent") is { IsNull: false } rootPtr)
            {
                nativeByName.TryGetValue(rootPtr.Name, out root);
            }
            root ??= created.FirstOrDefault(c => !parented.Contains(c));
            if (root == null) return;

            foreach (var component in created)
            {
                if (component != root && !parented.Contains(component)) root.Children.Add(component);
            }
        }

        void BuildOverrideTable()
        {
            // base first so that the most-derived override wins
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                var handler = chain[i].InheritableComponentHandler?.Load<UInheritableComponentHandler>();
                if (handler == null) continue;

                foreach (var record in handler.Records)
                {
                    if (record.ComponentTemplate is not { IsNull: false } template) continue;

                    var key = record.ComponentKey;
                    if (key.AssociatedGuid.IsValid())
                    {
                        overridesByGuid[key.AssociatedGuid] = template;
                    }

                    var ownerName = key.OwnerClass?.Name;
                    if (ownerName != null && key.SCSVariableName is { IsNone: false } variable)
                    {
                        overridesByKey[(ownerName, variable.Text)] = template;
                    }
                }
            }
        }

        void ExecuteConstructionScripts()
        {
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                var owner = chain[i];
                var script = owner.SimpleConstructionScript?.Load<USimpleConstructionScript>();
                if (script == null) continue;

                var processedAny = false;
                foreach (var nodePtr in script.RootNodes)
                {
                    if (nodePtr is not { IsNull: false }) continue;

                    processedAny = true;
                    ProcessNode(nodePtr, owner, null);
                }

                // the default scene root only exists when nothing else can be the root
                if (!processedAny && root is null && script.DefaultSceneRootNode is { IsNull: false } defaultRoot)
                {
                    ProcessNode(defaultRoot, owner, null);
                }
            }
        }

        void ProcessNode(FPackageIndex nodePtr, UBlueprintGeneratedClass owner, SceneComponentDto? parentFromRecursion)
        {
            if (!visitedNodes.Add(nodePtr) || !nodePtr.TryLoad<USCS_Node>(out var node)) return;

            var variableName = node.InternalVariableName.Text;
            var template = ResolveTemplate(node, owner.Name, variableName);
            if (template is not { IsNull: false })
            {
                Log.Warning("Node {NodeName} has no component template, skipping", variableName);
                ProcessChildren(parentFromRecursion);
                return;
            }

            if (!byTemplatePtr.TryGetValue(template, out var component))
            {
                if (ComponentDto.Create(template, this) is not SceneComponentDto created)
                {
                    ProcessChildren(parentFromRecursion);
                    return;
                }

                component = created;
                var parent = ResolveNodeParent(node, parentFromRecursion);
                if (root is null && parent is null)
                {
                    root = component;
                }
                else
                {
                    (parent ?? root)?.Children.Add(component);
                }

                byTemplatePtr[template] = component;
                scsByVariable[(owner.Name, variableName)] = component;
                scsByName.TryAdd(variableName, component);

                if (template.TryLoad<UChildActorComponent>(out var childActorComponent))
                {
                    TryCreateChildActor(childActorComponent, component);
                }
            }

            ProcessChildren(component);

            void ProcessChildren(SceneComponentDto? parent)
            {
                foreach (var childPtr in node.ChildNodes)
                {
                    if (childPtr is { IsNull: false }) ProcessNode(childPtr, owner, parent);
                }
            }
        }

        FPackageIndex? ResolveTemplate(USCS_Node node, string ownerName, string variableName)
        {
            if (node.VariableGuid.IsValid() && overridesByGuid.TryGetValue(node.VariableGuid, out var byGuid))
                return byGuid;
            if (overridesByKey.TryGetValue((ownerName, variableName), out var byKey))
                return byKey;
            return node.ComponentTemplate;
        }

        SceneComponentDto? ResolveNodeParent(USCS_Node node, SceneComponentDto? parentFromRecursion)
        {
            if (node.GetOrDefault<FName?>("ParentComponentOrVariableName") is not { IsNone: false } parentName)
                return parentFromRecursion;

            var name = parentName.Text;
            if (node.GetOrDefault("bIsParentComponentNative", false))
            {
                if (nativeByName.TryGetValue(name, out var native)) return native;
            }
            else
            {
                if (node.GetOrDefault<FName?>("ParentComponentOwnerClassName") is { IsNone: false } ownerClassName &&
                    scsByVariable.TryGetValue((ownerClassName.Text, name), out var scoped))
                    return scoped;
                if (scsByName.TryGetValue(name, out var byName)) return byName;
                if (nativeByName.TryGetValue(name, out var native)) return native;
            }

            Log.Warning("Node {NodeName} references unknown parent {Parent}, attaching to root", node.InternalVariableName.Text, name);
            return null;
        }

        void TryCreateChildActor(UChildActorComponent childActorComponent, SceneComponentDto component)
        {
            if (depth >= MaxChildActorDepth)
            {
                Log.Warning("Child actor depth limit reached at {Component}", component.Name);
                return;
            }

            if (childActorComponent.GetOrDefault<FPackageIndex?>("ChildActorClass") is not { IsNull: false } classPtr ||
                !classPtr.TryLoad<UBlueprintGeneratedClass>(out var childClass))
                return;

            var childKey = childClass.GetPathName();
            if (childKey == classKey || ancestry.Contains(childKey))
            {
                Log.Warning("Child actor {Class} would recurse into itself, skipping", childClass.Name);
                return;
            }

            var child = new BlueprintActorDto(childClass, new HashSet<string>(ancestry, StringComparer.Ordinal) { classKey }, depth + 1);
            if (child.RootComponent is not { } childRoot) return;

            // the child actor is spawned at the component's transform
            childRoot.Transform = FTransform.Identity;
            component.AttachedActors.Add(child);
        }
    }
}
