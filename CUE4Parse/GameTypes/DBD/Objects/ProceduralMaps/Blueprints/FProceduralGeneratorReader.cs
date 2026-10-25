using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;

internal static class FProceduralGeneratorReader
{
    public readonly record struct FRule(FName Tag, string Component, bool WhenGeneratorSpawned, FActorSpawnerProperties[] Elements);

    public static List<FRule> Read(UBlueprintGeneratedClass blueprint)
    {
        var rules = new List<FRule>();
        if (blueprint.Owner?.Provider?.ReadScriptData != true)
            return rules;

        var visited = new HashSet<UBlueprintGeneratedClass>();
        for (var current = blueprint; current != null && visited.Add(current); current = current.SuperStruct?.Load() as UBlueprintGeneratedClass)
        {
            var handler = current.Children.FirstOrDefault(child => child.Name == "OnSetSpawnObject")?.Load<UFunction>();
            if (handler?.ScriptBytecode is not { } script)
                continue;

            var spawnPoint = script.OfType<EX_LetValueOnPersistentFrame>().FirstOrDefault(assignment
                => assignment.AssignmentExpression is EX_LocalVariable variable && variable.Variable.ToString() == "tileSpawnPoint")?.DestinationProperty.ToString();
            if (spawnPoint == null)
                continue;

            var spawners = FProceduralComponentReader.ReadSpawners(blueprint);
            var doors = FProceduralComponentReader.ReadComponents<UChildActorComponent>(blueprint);
            foreach (var call in script.OfType<EX_LocalFinalFunction>())
            {
                if (call.StackNode.Load<UFunction>()?.ScriptBytecode is not { } graph)
                    continue;

                FName tag = default;
                foreach (var expression in graph)
                {
                    switch (expression)
                    {
                        case EX_LetBool { Assignment: EX_Context { ObjectExpression: EX_LocalVariable source, ContextExpression: EX_FinalFunction check } } when source.Variable.ToString() == spawnPoint && check.StackNode.Name == "ComponentHasTag":
                            tag = check.Parameters is [EX_NameConst name] ? name.Value : default;
                            break;
                        case EX_Context { ObjectExpression: EX_InstanceVariable target, ContextExpression: EX_FinalFunction select } when !tag.IsNone && select.StackNode.Name == "AuthoritySelect" && select.Parameters is [EX_True or EX_False]:
                            var component = target.Variable.ToString();
                            var templateName = component + USimpleConstructionScript.ComponentTemplateNameSuffix;
                            var spawner = spawners.FirstOrDefault(entry => entry.Spawner.Name == component || entry.Spawner.Name == templateName).Spawner;
                            if (spawner != null)
                                rules.Add(new FRule(tag, component, true, select.Parameters[0] is EX_True ? spawner.ActivatedSceneElement : spawner.DeactivatedSceneElement));
                            break;
                        case EX_LocalVirtualFunction { Parameters: [EX_InstanceVariable target] } open when !tag.IsNone && OpensDoor(current.Children.FirstOrDefault(child => child.Name == open.VirtualFunctionName)?.Load<UFunction>()):
                            var doorName = target.Variable.ToString();
                            var doorTemplate = doorName + USimpleConstructionScript.ComponentTemplateNameSuffix;
                            var door = doors.FirstOrDefault(entry => entry.Component.Name == doorName || entry.Component.Name == doorTemplate).Component;
                            if (RemovesDoorWhenOpened(door))
                                rules.Add(new FRule(tag, doorName, false, []));
                            break;
                    }
                }
            }
        }
        return rules;
    }

    private static bool OpensDoor(UFunction? function) => function?.ScriptBytecode?.Any(instruction => instruction is EX_Context
    {
        ObjectExpression: EX_InterfaceContext,
        ContextExpression: EX_LocalVirtualFunction { VirtualFunctionName.Text: "InitAsOpen", Parameters: [] }
    }) == true;

    private static bool RemovesDoorWhenOpened(UChildActorComponent? door)
    {
        if (door?.GetOrDefault<FPackageIndex?>("ChildActorClass")?.Load<UBlueprintGeneratedClass>() is not { } blueprint)
            return false;

        // Pig's map unused generator doors initialize as open by destroying the door actor
        return blueprint.Children.FirstOrDefault(child => child.Name == "InitializeAsOpen")?.Load<UFunction>()?.ScriptBytecode is [EX_VirtualFunction { VirtualFunctionName.Text: "K2_DestroyActor", Parameters: [] }, EX_Return, EX_EndOfScript];
    }
}
