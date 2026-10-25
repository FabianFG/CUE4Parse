using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;

internal static class FProceduralRandomizerReader
{
    public static Dictionary<string, string[]> Read(UBlueprintGeneratedClass blueprint, FProceduralRandomStream random)
    {
        var bindings = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var spawnerNames = FProceduralComponentReader.ReadNamedComponents<UActorSpawner>(blueprint).ToDictionary(entry => entry.Component, entry => entry.Name);
        foreach (var (name, randomizer, _) in FProceduralComponentReader.ReadNamedComponents<UObjectRandomizer>(blueprint))
        {
            bindings[name] = [.. randomizer.Spawners.Select(spawner => spawner.TryLoad<UActorSpawner>(out var component) && spawnerNames.TryGetValue(component, out var variable) ? variable : GetName(spawner.Name))];
        }

        if (bindings.Count == 0 || blueprint.Owner?.Provider?.ReadScriptData != true)
            return bindings;

        var classes = new Stack<UBlueprintGeneratedClass>();
        var visited = new HashSet<UBlueprintGeneratedClass>();
        for (var current = blueprint; current != null && visited.Add(current); current = current.SuperStruct?.Load() as UBlueprintGeneratedClass)
            classes.Push(current);

        foreach (var current in classes)
        {
            if (current.Children.FirstOrDefault(child => child.Name == "UserConstructionScript")?.Load<UFunction>()?.ScriptBytecode is not { } script)
                continue;

            var randomBooleans = script.OfType<EX_LetBool>().Where(assignment => assignment.Variable is EX_LocalVariable &&
                assignment.Assignment is EX_CallMath { StackNode.Name: "RandomBoolFromStream", Parameters: [EX_InstanceVariable stream] } &&
                stream.Variable.ToString() == "SharedTileRandomizer").Select(assignment => ((EX_LocalVariable) assignment.Variable).Variable.ToString()).ToHashSet(StringComparer.Ordinal);

            var followBranches = randomBooleans.Count > 0 && script.OfType<EX_JumpIfNot>().All(jump => jump.BooleanExpression is EX_LocalVariable condition && randomBooleans.Contains(condition.Variable.ToString()));
            var booleans = new Dictionary<string, bool>(StringComparer.Ordinal);
            var arrays = new Dictionary<string, string[]>(StringComparer.Ordinal);
            for (var index = 0; index < script.Length; index++)
            {
                switch (script[index])
                {
                    case EX_LetBool { Variable: EX_LocalVariable target } when followBranches && randomBooleans.Contains(target.Variable.ToString()):
                        booleans[target.Variable.ToString()] = random.NextFloat() >= 0.5f;
                        break;
                    case EX_JumpIfNot { BooleanExpression: EX_LocalVariable condition } jump when followBranches && booleans.TryGetValue(condition.Variable.ToString(), out var value):
                        if (!value)
                            index = FindJumpTarget(jump, index) - 1;
                        break;
                    case EX_Jump { Token: EExprToken.EX_Jump } jump when followBranches:
                        index = FindJumpTarget(jump, index) - 1;
                        break;
                    case EX_SetArray { AssigningProperty: EX_LocalVariable target } array:
                        arrays[target.Variable.ToString()] = [.. array.Elements.OfType<EX_InstanceVariable>().Select(value => value.Variable.ToString())];
                        break;
                    case EX_Let { Variable: EX_Context { ObjectExpression: EX_InstanceVariable target, ContextExpression: EX_InstanceVariable property }, Assignment: EX_LocalVariable source }
                        when property.Variable.ToString() == nameof(UObjectRandomizer.Spawners) && bindings.ContainsKey(target.Variable.ToString()) && arrays.TryGetValue(source.Variable.ToString(), out var spawners):
                        bindings[target.Variable.ToString()] = spawners;
                        break;
                }
            }

            int FindJumpTarget(EX_Jump jump, int index)
            {
                var target = Array.FindIndex(script, index + 1, expression => expression.StatementIndex == jump.CodeOffset);
                return target >= 0 ? target : script.Length;
            }
        }

        return bindings;
    }

    internal static string GetName(string name) => name.EndsWith(USimpleConstructionScript.ComponentTemplateNameSuffix, StringComparison.Ordinal) ? name[..^USimpleConstructionScript.ComponentTemplateNameSuffix.Length] : name;
}
