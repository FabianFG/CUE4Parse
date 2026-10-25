using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;

internal static class FProceduralBorderReader
{
    public static Dictionary<string, EDBDDirection> Read(UBlueprintGeneratedClass blueprint)
    {
        var result = new Dictionary<string, EDBDDirection>(StringComparer.Ordinal);
        var classes = new Stack<UBlueprintGeneratedClass>();
        var visited = new HashSet<UBlueprintGeneratedClass>();
        for (var current = blueprint; current != null && visited.Add(current); current = current.SuperStruct?.Load() as UBlueprintGeneratedClass)
            classes.Push(current);

        if (!classes.Any(current => current.SuperStruct?.Name == "BorderTile"))
            return result;

        var readScriptData = blueprint.Owner?.Provider?.ReadScriptData == true;
        foreach (var current in classes)
        {
            var borders = current.ClassDefaultObject?.Load()?.GetOrDefault<FBorderElements[]>("Borders", []) ?? [];
            foreach (var border in borders)
            {
                foreach (var element in border.Elements)
                {
                    var name = element.Name;
                    if (name.EndsWith(USimpleConstructionScript.ComponentTemplateNameSuffix, StringComparison.Ordinal))
                        name = name[..^USimpleConstructionScript.ComponentTemplateNameSuffix.Length];
                    result[name] = border.Direction;
                }
            }

            if (!readScriptData)
                continue;

            foreach (var child in current.Children)
            {
                if (child.Name != "UserConstructionScript" || child.Load<UFunction>()?.ScriptBytecode is not { } script)
                    continue;

                ReadAssignments(script, result);
            }
        }

        return result;
    }

    private sealed class BorderAssignment
    {
        public EDBDDirection? Direction;
        public string[] Elements = [];
    }

    // Read only what is needed to populate Borders
    private static void ReadAssignments(KismetExpression[] script, Dictionary<string, EDBDDirection> result)
    {
        var arrays = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var borders = new Dictionary<string, BorderAssignment>(StringComparer.Ordinal);
        foreach (var expression in script)
        {
            switch (expression)
            {
                case EX_SetArray { AssigningProperty: EX_LocalVariable target } array:
                    arrays[target.Variable.ToString()] = array.Elements.OfType<EX_VariableBase>().Select(value => value.Variable.ToString()).ToArray();
                    break;
                case EX_Let { Variable: EX_StructMemberContext { StructExpression: EX_LocalVariable target } member } assignment:
                    var name = target.Variable.ToString();
                    if (!borders.TryGetValue(name, out var border))
                        borders[name] = border = new BorderAssignment();

                    if (member.Property.ToString() == "Direction" && assignment.Assignment is EX_ByteConst direction)
                    {
                        border.Direction = (EDBDDirection) direction.Value;
                    }
                    else if (member.Property.ToString() == "Elements" && assignment.Assignment is EX_LocalVariable source && arrays.TryGetValue(source.Variable.ToString(), out var values))
                    {
                        border.Elements = values;
                    }
                    break;
                case EX_Let { Variable: EX_InstanceVariable target, Assignment: EX_LocalVariable source } when target.Variable.ToString() == "Borders":
                    if (!arrays.TryGetValue(source.Variable.ToString(), out var borderNames))
                        break;

                    result.Clear();
                    foreach (var borderName in borderNames)
                    {
                        if (!borders.TryGetValue(borderName, out var assignedBorder) || assignedBorder.Direction is not { } assignedDirection)
                            continue;

                        foreach (var spawner in assignedBorder.Elements)
                        {
                            result[spawner] = assignedDirection;
                        }
                    }
                    break;
            }
        }
    }
}
