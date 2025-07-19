using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject;

[SkipObjectRegistration]
public class UClass : UStruct
{
    /** Used to check if the class was cooked or not */
    public bool bCooked;

    /** Class flags; See EClassFlags for more information */
    public EClassFlags ClassFlags; // EClassFlags

    /** The required type for the outer of instances of this class */
    public FPackageIndex ClassWithin;

    /** This is the blueprint that caused the generation of this class, or null if it is a native compiled-in class */
    public FPackageIndex ClassGeneratedBy;

    /** Which Name.ini file to load Config variables out of */
    public FName ClassConfigName;

    /** The class default object; used for delta serialization and object initialization */
    public FPackageIndex ClassDefaultObject;

    /** Map of all functions by name contained in this class */
    public Dictionary<FName, FPackageIndex /*UFunction*/> FuncMap;

    /**
     * The list of interfaces which this class implements, along with the pointer property that is located at the offset of the interface's vtable.
     * If the interface class isn't native, the property will be null.
     */
    public FImplementedInterface[] Interfaces;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game == EGame.GAME_AWayOut) Ar.Position += 4;

        // serialize the function map
        FuncMap = new Dictionary<FName, FPackageIndex>();
        var funcMapNum = Ar.Read<int>();
        for (var i = 0; i < funcMapNum; i++)
        {
            FuncMap[Ar.ReadFName()] = new FPackageIndex(Ar);
        }

        // Class flags first.
        ClassFlags = Ar.Read<EClassFlags>();

        // Variables.
        if (Ar.Game is EGame.GAME_StarWarsJediFallenOrder or EGame.GAME_StarWarsJediSurvivor or EGame.GAME_AshesOfCreation) Ar.Position += 4;
        ClassWithin = new FPackageIndex(Ar);
        ClassConfigName = Ar.ReadFName();

        ClassGeneratedBy = new FPackageIndex(Ar);

        // Load serialized interface classes
        Interfaces = Ar.ReadArray(() => new FImplementedInterface(Ar));

        var bDeprecatedForceScriptOrder = Ar.ReadBoolean();
        var dummy = Ar.ReadFName();

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.ADD_COOKED_TO_UCLASS)
        {
            bCooked = Ar.ReadBoolean();
        }

        // Defaults.
        ClassDefaultObject = new FPackageIndex(Ar);
    }

    public Assets.Exports.UObject? ConstructObject(EObjectFlags flags)
    {
        var type = ObjectTypeRegistry.Get(Name);
        if (type is null && this is UBlueprintGeneratedClass && flags.HasFlag(EObjectFlags.RF_ClassDefaultObject)) type = typeof(Assets.Exports.UObject);
        if (type != null)
        {
            try
            {
                var instance = Activator.CreateInstance(type);
                if (instance is Assets.Exports.UObject obj)
                {
                    return obj;
                }
                else
                {
                    Log.Warning("Class {Type} did have a valid constructor but does not inherit UObject", type);
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, "Class {Type} could not be constructed", type);
            }
        }

        return null;
    }

    // ignore this, this will not be here in the final verison.
    public static string? somerandomstupidloop(string json, string functionName, string metaKey)
    {
        var root = JArray.Parse(json);

        foreach (var item in root)
        {
            var functions = item["Properties"]?["FunctionsMetaData"] as JArray;
            if (functions == null) continue;

            foreach (var func in functions)
            {
                var key = (string?) func["Key"];
                if (key == null || !key.Equals(functionName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var objectMetaArray = func["Value"]?["ObjectMetaData"]?["ObjectMetaData"] as JArray;
                if (objectMetaArray == null) return null;

                foreach (var meta in objectMetaArray)
                {
                    var metaKeyName = (string?) meta["Key"];
                    if (metaKeyName != null && metaKeyName.Equals(metaKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return (string?) meta["Value"];
                    }
                }
            }
        }

        return null;
    }

    public string DecompileBlueprintToPseudo(string editordata)
    {
        var derivedClass = BlueprintDecompilerUtils.GetClassWithPrefix(this);
        var accessSpecifier = Flags.HasFlag(EObjectFlags.RF_Public) ? "public" : "private";

        var superStruct = SuperStruct.Load<UStruct>();
        var baseClass = BlueprintDecompilerUtils.GetClassWithPrefix(superStruct);

        ClassDefaultObject.TryLoad(out var classDefaultObject);
        bool emptyClass = Properties.Count == 0 && ChildProperties.Length == 0 && FuncMap.Count == 0 && classDefaultObject.Properties.Count == 0;

        var stringBuilder = new CustomStringBuilder();

        if (emptyClass)
        {
            stringBuilder.Append($"class {derivedClass} : {accessSpecifier} {baseClass}");
            return stringBuilder.ToString();
        }

        stringBuilder.AppendLine($"class {derivedClass} : {accessSpecifier} {baseClass}");
        stringBuilder.OpenBlock();

        var existingVariables = new HashSet<string>();
        var variables = new Dictionary<string, EAccessMode>();

        foreach (var property in Properties)
        {
            if (!existingVariables.Add(property.Name.Text))
                continue;

            if (!BlueprintDecompilerUtils.GetPropertyTagVariable(property, out var variableType, out var variableValue))
            {
                throw new NotImplementedException($"Unable to get property type or value for {property.PropertyType.ToString()} of type {property.Name.ToString()}");
            }

            var variableExpression = $"{variableType} {property.Name.ToString()} = {variableValue};";
            variables[variableExpression] = EAccessMode.Public;
        }

        if (classDefaultObject is not null)
        {
            foreach (var property in classDefaultObject.Properties)
            {
                if (!existingVariables.Add(property.Name.Text))
                    continue;

                // TODO move this shit to the fucking function itself so fucking delegate is not a bitch ffs
                if (!BlueprintDecompilerUtils.GetPropertyTagVariable(property, out var variableType, out var variableValue))
                {
                    throw new NotImplementedException($"Unable to get property type or value for {property.PropertyType.ToString()} of type {property.Name.ToString()}");
                }

                var variableExpression = $"{variableType} {property.Name.ToString()} = {variableValue};";
                variables[variableExpression] = EAccessMode.Protected;
            }
        }

        foreach (var childProperty in ChildProperties)
        {
            if (childProperty is not FProperty property)
                continue;

            if (!existingVariables.Add(property.Name.Text))
                continue;

            var (variableValue, variableType) = BlueprintDecompilerUtils.GetPropertyType(property);
            if (variableType is null)
                continue;

            var value = variableValue is null ? string.Empty : $" = {variableValue}";
            var variableExpression = $"{variableType} {property.Name.Text}{value};";

            var accessMode = property.PropertyFlags.HasFlag(EPropertyFlags.BlueprintVisible) ? EAccessMode.Protected : EAccessMode.Public;
            variables[variableExpression] = EAccessMode.Private;
        }

        foreach (var group in variables.GroupBy(pair => pair.Value))
        {
            stringBuilder.DecreaseIndentation();
            stringBuilder.AppendLine(group.Key.ToString().ToLower() + ":");
            stringBuilder.IncreaseIndentation();

            foreach (var variable in group.Select(pair => pair.Key))
            {
                stringBuilder.AppendLine(variable);
            }
        }

        if (FuncMap.Count > 0) stringBuilder.AppendLine();

        var totalFuncMapCount = FuncMap.Count;
        var index = 1;

        // disable or enable funcs (dbg)
        if (true)
        {
            foreach (var (key, value) in FuncMap)
            {
                if (!value.TryLoad(out var export) || export is not UFunction function)
                    continue;

                var parametersList = new List<string>();

                var returnType = "void";
                foreach (var childProperty in function.ChildProperties)
                {
                    if (childProperty is not FProperty property || !property.PropertyFlags.HasFlag(EPropertyFlags.Parm))
                        continue;

                    var (_, variableType) = BlueprintDecompilerUtils.GetPropertyType(property);
                    if (variableType is null)
                        continue;

                    if (property.PropertyFlags.HasFlag(EPropertyFlags.ReturnParm))
                    {
                        returnType = variableType;
                        continue;
                    }

                    var parameterExpression = $"{variableType} {property.Name.Text}";
                    parametersList.Add(parameterExpression);
                }

                var parameters = string.Join(", ", parametersList);
                var functionExpression = $"{returnType} {key.Text}({parameters})";

                var functionStringBuilder = new CustomStringBuilder();
                if (editordata.Length > 0)
                {
                    string? Category = somerandomstupidloop(editordata, key.Text, "Category");
                    string? ToolTip = somerandomstupidloop(editordata, key.Text, "ToolTip");
                    string? ModuleRelativePath = somerandomstupidloop(editordata, key.Text, "ModuleRelativePath");
                    if (Category != null) functionStringBuilder.AppendLine($"// Category: {Category}");
                    if (ToolTip != null)
                        functionStringBuilder.AppendLine(string.Join(Environment.NewLine,
                            ToolTip.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                                .Select(line => $"// {line}")));
                    if (ModuleRelativePath != null)
                        functionStringBuilder.AppendLine($"// ModuleRelativePath: {ModuleRelativePath}");
                }

                string flags =
                    $"({string.Join(", ", function.FunctionFlags.ToString().Split('|').Select(f => f.Trim().Replace("FUNC_", "")))})";
                functionStringBuilder.AppendLine($"// {flags}");
                functionStringBuilder.AppendLine(functionExpression);
                functionStringBuilder.OpenBlock();

                foreach (var kismetExpression in function.ScriptBytecode)
                {
                    if (kismetExpression is EX_Nothing or EX_NothingInt32 or EX_EndFunctionParms or EX_EndStructConst
                        or EX_EndArray or EX_EndArrayConst or EX_EndSet or EX_EndMap or EX_EndMapConst or EX_EndSetConst
                        or EX_DeprecatedOp4A or EX_EndOfScript or EX_PushExecutionFlow or EX_JumpIfNot
                        or EX_ComputedJump or EX_PopExecutionFlow)
                        continue;

                    var lineExpression = $"{BlueprintDecompilerUtils.GetLineExpression(kismetExpression)};";

#if DEBUG
                    lineExpression += $" // {kismetExpression.GetType().Name}";
#endif

                    functionStringBuilder.AppendLine($"{lineExpression}");

                    if (!lineExpression.StartsWith("return"))
                    {
                        functionStringBuilder.AppendLine();
                    }
                }

                functionStringBuilder.CloseBlock();

                var functionBlock = functionStringBuilder.ToString();
                stringBuilder.AppendLine(functionBlock);
                if (index < totalFuncMapCount) stringBuilder.AppendLine();

                index++;
            }
        }

        stringBuilder.CloseBlock("};");

        return stringBuilder.ToString();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (FuncMap is { Count: > 0 })
        {
            writer.WritePropertyName("FuncMap");
            serializer.Serialize(writer, FuncMap);
        }

        if (ClassFlags != EClassFlags.CLASS_None)
        {
            writer.WritePropertyName("ClassFlags");
            writer.WriteValue(ClassFlags.ToStringBitfield());
        }

        if (ClassWithin is { IsNull: false })
        {
            writer.WritePropertyName("ClassWithin");
            serializer.Serialize(writer, ClassWithin);
        }

        if (!ClassConfigName.IsNone)
        {
            writer.WritePropertyName("ClassConfigName");
            serializer.Serialize(writer, ClassConfigName);
        }

        if (ClassGeneratedBy is { IsNull: false })
        {
            writer.WritePropertyName("ClassGeneratedBy");
            serializer.Serialize(writer, ClassGeneratedBy);
        }

        if (Interfaces is { Length: > 0 })
        {
            writer.WritePropertyName("Interfaces");
            serializer.Serialize(writer, Interfaces);
        }

        if (bCooked)
        {
            writer.WritePropertyName("bCooked");
            writer.WriteValue(bCooked);
        }

        if (ClassDefaultObject is { IsNull: false })
        {
            writer.WritePropertyName("ClassDefaultObject");
            serializer.Serialize(writer, ClassDefaultObject);
        }
    }

    public class FImplementedInterface
    {
        /** the interface class */
        public FPackageIndex Class;

        /** the pointer offset of the interface's vtable */
        public int PointerOffset;

        /** whether or not this interface has been implemented via K2 */
        public bool bImplementedByK2;

        public FImplementedInterface(FAssetArchive Ar)
        {
            Class = new FPackageIndex(Ar);
            PointerOffset = Ar.Read<int>();
            bImplementedByK2 = Ar.ReadBoolean();
        }
    }
}

public enum EAccessMode : byte
{
    Public,
    Protected,
    Private,
}
