using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;
using CUE4Parse.UE4.Objects.UObject.Editor;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
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

        _ = Ar.ReadBoolean();
        _ = Ar.ReadFName();

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
        if (type is null && this is UBlueprintGeneratedClass && flags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
        {
            type = typeof(Assets.Exports.UObject);
        }

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

    public string DecompileBlueprintToPseudo(UClassCookedMetaData? cookedMetaData = null)
    {
        var derivedClass = BlueprintDecompilerUtils.GetClassWithPrefix(this);
        var baseClass = BlueprintDecompilerUtils.GetClassWithPrefix(SuperStruct.Load<UStruct>());
        var accessSpecifier = Flags.HasFlag(EObjectFlags.RF_Public) ? "public" : "private";

        var classDefaultObject = ClassDefaultObject.Load();
        bool emptyClass = Properties.Count == 0 && (ChildProperties?.Length ?? 0) == 0 && FuncMap.Count == 0 && (classDefaultObject?.Properties.Count ?? 0) == 0;
        
        var c = $"class {derivedClass} : {accessSpecifier} {baseClass}";
        if (emptyClass) return $"{c} {{ }};";

        var stringBuilder = new CustomStringBuilder();
        stringBuilder.AppendLine(c);
        stringBuilder.OpenBlock();

        var distinct = new HashSet<string>();
        var variables = new Dictionary<string, EAccessMode>();
        
        var combined = Properties.Concat(classDefaultObject?.Properties ?? []).Concat(classDefaultObject?.SerializedSparseClassData?.Properties ?? []);
        foreach (var property in combined)
        {
            if (!distinct.Add(property.Name.Text)) continue;
            variables.TryAdd(property.GetCppVariable(), EAccessMode.Public); // should always be public
        }
        foreach (var childProperty in ChildProperties ?? [])
        {
            if (childProperty is not FProperty property || !distinct.Add(property.Name.Text))
                continue;

            var (variableValue, variableType) = BlueprintDecompilerUtils.GetPropertyType(property);
            if (variableType is null)
                continue;

            var value = variableValue is null ? string.Empty : $" = {variableValue}";
            variables.TryAdd($"{variableType} {property.Name.Text}{value};", property.GetAccessMode());
        }

        foreach (var group in variables.GroupBy(pair => pair.Value))
        {
            stringBuilder.DecreaseIndentation();
            stringBuilder.AppendLine(group.Key.ToString().ToLower() + ":");
            stringBuilder.IncreaseIndentation();

            foreach (var variable in group)
            {
                stringBuilder.AppendLine(variable.Key);
            }
        }

        var totalFuncMapCount = FuncMap.Count;
        if (totalFuncMapCount > 0) stringBuilder.AppendLine();


        var jumpCodeOffsetsMap = new Dictionary<string, List<int>>();
        foreach (var value in FuncMap.Values.Reverse())
        {
            if (!value.TryLoad(out var export) || export is not UFunction function)
                continue;
            if (function?.ScriptBytecode == null)
                continue;
            foreach (var expression in function.ScriptBytecode)
            {
                string? label = null;
                int? offset = null;

                switch (expression)
                {
                    case EX_Jump jump:
                        label = jump.ObjectName;
                        offset = (int)jump.CodeOffset;
                        break;
                    case EX_LocalFinalFunction final:
                        label = final.StackNode.Name.Split('.').Last().Split('[')[0];
                        if (final.Parameters is [EX_IntConst intConst])
                            offset = intConst.Value;
                        break;
                }

                if (!string.IsNullOrEmpty(label) && offset.HasValue)
                {
                    if (!jumpCodeOffsetsMap.TryGetValue(label, out var list))
                        jumpCodeOffsetsMap[label] = list = [];

                    list.Add(offset.Value);
                }
            }
        }

        var index = 1;
        foreach (var (key, value) in FuncMap)
        {
            if (!value.TryLoad(out var export) || export is not UFunction function)
                continue;

            var parametersList = new List<string>();

            var returnType = "void";
            foreach (var childProperty in function.ChildProperties ?? [])
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

            var functionStringBuilder = new CustomStringBuilder();
            if (cookedMetaData != null && cookedMetaData.FunctionsMetaData.TryGetValue(key.Text, out var editorData) && editorData != null)
            {
                if (editorData.Value.ObjectMetaData.ObjectMetaData.TryGetValue("Category", out var category) && category != null)
                {
                    functionStringBuilder.AppendLine($"// Category: {category}");
                }
                if (editorData.Value.ObjectMetaData.ObjectMetaData.TryGetValue("ToolTip", out var tooltip) && tooltip != null)
                {
                    functionStringBuilder.AppendLine(string.Join(Environment.NewLine, tooltip.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).Select(line => $"// {line}")));
                }
                if (editorData.Value.ObjectMetaData.ObjectMetaData.TryGetValue("ModuleRelativePath", out var moduleRelativePath) && moduleRelativePath != null)
                {
                    functionStringBuilder.AppendLine($"// ModuleRelativePath: {moduleRelativePath}");
                }
            }

            var flags = $"({string.Join(", ", function.FunctionFlags.ToString().Split('|').Select(f => f.Trim().Replace("FUNC_", "")))})";
            var functionExpression = $"{function.GetAccessMode().ToString().ToLower()} {returnType} {key.Text}({string.Join(", ", parametersList)})";
            functionStringBuilder.AppendLine($"// {flags}");
            functionStringBuilder.AppendLine(functionExpression);
            functionStringBuilder.OpenBlock();

            if (function?.ScriptBytecode == null || function?.ScriptBytecode.Length == 0)
            {
                functionStringBuilder.AppendLine("// No Script Bytecode");
                stringBuilder.CloseBlock("};");
                return stringBuilder.ToString();
            }
            var jumpCodeOffsets = jumpCodeOffsetsMap.TryGetValue(function.Name, out var jumpList) ? jumpList : [];
            foreach (var kismetExpression in function.ScriptBytecode)
            {
                if (kismetExpression is EX_Nothing or EX_NothingInt32 or EX_EndFunctionParms or EX_EndStructConst or EX_EndArray or EX_EndArrayConst or EX_EndSet or EX_EndMap or EX_EndMapConst or EX_EndSetConst or EX_EndOfScript or EX_PushExecutionFlow or EX_PopExecutionFlow)
                    continue;

                if (jumpCodeOffsets.Contains(kismetExpression.StatementIndex))
                    functionStringBuilder.AppendLine($"Label_{kismetExpression.StatementIndex}:");

                var expression = BlueprintDecompilerUtils.GetLineExpression(kismetExpression);

                if (!string.IsNullOrWhiteSpace(expression))
                {
                    var lineExpression = $"{expression};";

#if DEBUG
                    lineExpression += $" // {kismetExpression.GetType().Name}";
#endif

                    functionStringBuilder.AppendLine(lineExpression);
                    if (!lineExpression.StartsWith("return"))
                    {
                        functionStringBuilder.AppendLine();
                    }
                }
            }

            functionStringBuilder.CloseBlock();

            stringBuilder.AppendLine(functionStringBuilder.ToString());
            if (index < totalFuncMapCount) stringBuilder.AppendLine();

            index++;
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

        /** whether this interface has been implemented via K2 */
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
