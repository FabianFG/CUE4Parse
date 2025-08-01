using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Ai;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.Engine.GameFramework;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;

public static class BlueprintDecompilerUtils
{
    public static string GetClassWithPrefix(UStruct? prefixClassStruct)
    {
        var prefix = GetPrefix(prefixClassStruct);
        return $"{prefix}{prefixClassStruct?.Name}";
    }

    private static string GetPrefix(UStruct? prefixStruct)
    {
        while (true)
        {
            var structName = prefixStruct?.Name;
            if (!string.IsNullOrEmpty(structName))
            {
                if (structName.Contains("Actor"))
                    return "A";
                if (structName.Contains("Interface"))
                    return "I";
                if (structName.Contains("Object"))
                    return "U";
            }

            if (prefixStruct?.SuperStruct is null || !prefixStruct.SuperStruct.TryLoad(out prefixStruct))
                break;
        }

        return "U";
    }

    private static bool IsPointer(FProperty property) => property.PropertyFlags.HasFlag(EPropertyFlags.ReferenceParm) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags
                                                             .InstancedReference) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags
                                                             .ContainsInstancedReference) ||
                                                         property.GetType() == typeof(FObjectProperty);

    public static (string?, string?) GetPropertyType(FProperty property)
    {
        string? type = null;
        string? value = null;

        var propertyFlags = property.PropertyFlags;
        if (propertyFlags.HasFlag(EPropertyFlags.ConstParm))
        {
            type += "const ";
        }

        switch (property)
        {
            case FObjectProperty objectProperty:
            {
                var classType = objectProperty.PropertyClass.Name;

                value = objectProperty.PropertyClass.ToString();
                type += $"class U{classType}";

                break;
            }
            case FArrayProperty arrayProperty:
            {
                var (innerValue, innerType) = GetPropertyType(arrayProperty.Inner!);

                var customStringBuilder = new CustomStringBuilder();
                customStringBuilder.OpenBlock("[");
                customStringBuilder.AppendLine(innerValue!);
                customStringBuilder.CloseBlock("]");

                value = customStringBuilder.ToString();
                type += $"TArray<{innerType}>";

                break;
            }
            case FStructProperty structProperty:
            {
                var structType = structProperty.Struct.Name;

                type += $"struct F{structType}";
                value = structProperty.Struct.ToString();
                break;
            }
            case FNumericProperty:
            {
                if (property is FByteProperty byteProperty && byteProperty.Enum.TryLoad(out var enumObj))
                {
                    type = enumObj.Name;
                }
                else
                {
                    type = property.GetType().Name.SubstringAfter("F").SubstringBefore("Property").ToLowerInvariant();
                }

                break;
            }
            case FInterfaceProperty interfaceProperty:
            {
                type = $"F{interfaceProperty.InterfaceClass.Name}";
                break;
            }
            case FBoolProperty boolProperty:
            {
                type = boolProperty.bIsNativeBool ? "bool" : "uint8";
                break;
            }
            case FStrProperty:
            case FVerseStringProperty: // hm
            {
                type = "FString";
                break;
            }
            case FTextProperty:
            {
                type = "FText";
                break;
            }
            case FNameProperty:
            {
                type = "FName";
                break;
            }
            case FMapProperty:
            {
                type = "FMap";
                break;
            }
            case FEnumProperty:
            {
                type = "Enum";
                break;
            }
            default:
            {
                Log.Warning("Property Value '{type}' is currently not supported", property.GetType().Name);
                break;
            }
        }

        if (IsPointer(property))
            type += "*";

        if (propertyFlags.HasFlag(EPropertyFlags.OutParm) && !propertyFlags.HasFlag(EPropertyFlags.ReturnParm))
            type += "&";

        return (value, type);
    }

    private static T GetGenericValue<T>(this FPropertyTag propertyTag) => (T)propertyTag.Tag?.GenericValue!;

    public static bool GetPropertyTagVariable(FPropertyTag propertyTag, out string type, out string value)
    {
        type = string.Empty;
        value = string.Empty;

        if (!Enum.TryParse<EPropertyType>(propertyTag.PropertyType.ToString(), out var propertyType))
        {
            Log.Warning("Unable to Parse {0} while trying to get PropertyEnum Type",
                propertyTag.PropertyType.ToString());
            return false;
        }

        switch (propertyType)
        {
            case EPropertyType.ByteProperty:
            {
                if (propertyTag.Tag?.GenericValue is FName name)
                {
                    var enumValue = name.ToString();

                    value = $"{enumValue}";
                    type = $"enum {enumValue.SubstringBefore("::")}";
                }
                else
                {
                    value = propertyTag.GetGenericValue<byte>().ToString();
                    type = "byte";
                }

                break;
            }
            case EPropertyType.BoolProperty:
            {
                type = "bool";
                value = propertyTag.GetGenericValue<bool>().ToString().ToLowerInvariant();
                break;
            }
            case EPropertyType.IntProperty:
            {
                type = "int32";
                value = propertyTag.GetGenericValue<int>().ToString();
                break;
            }
            case EPropertyType.FloatProperty:
            {
                type = "float";
                value = propertyTag.GetGenericValue<float>().ToString(CultureInfo.InvariantCulture);
                break;
            }
            case EPropertyType.ObjectProperty or EPropertyType.ClassProperty:
            {
                var pkgIndex = propertyTag.GetGenericValue<FPackageIndex>().ToString();
                if (pkgIndex is null or "0")
                {
                    type = "class UObject*";
                    value = "nullptr";
                }
                else
                {
                    type = $"class U{pkgIndex.SubstringBefore("'")}*";
                    value = $"\"{pkgIndex}\"";
                }

                break;
            }
            case EPropertyType.NameProperty:
            {
                type = "FName";
                value = $"FName(\"{propertyTag.GetGenericValue<FName>().ToString()}\")";
                break;
            }
            case EPropertyType.DoubleProperty:
            {
                type = "double";
                value = propertyTag.GetGenericValue<double>().ToString(CultureInfo.InvariantCulture);
                break;
            }
            case EPropertyType.ArrayProperty:
            {
                var scriptArray = propertyTag.GetGenericValue<UScriptArray>();
                if (scriptArray == null || scriptArray.Properties == null || scriptArray.InnerType == null)
                {
                    value = "{}";
                    type = "TArray<unknown>";
                    break;
                }

                if (scriptArray.Properties.Count == 0)
                {
                    value = "{}";
                    var innerType = scriptArray.InnerType switch
                    {
                        "IntProperty" => "int",
                        "Int8Property" => "int8",
                        "Int16Property" => "int16",
                        "Int64Property" => "int64",
                        "UInt16Property" => "uint16",
                        "UInt32Property" => "uint32",
                        "UInt64Property" => "uint64",
                        "ByteProperty" => "byte",
                        "BoolProperty" => "bool",
                        "StrProperty" => "string",
                        "DoubleProperty" => "double",
                        "NameProperty" => "FName",
                        "TextProperty" => "FText",
                        "FloatProperty" => "float",
                        "SoftObjectProperty" or "AssetObjectProperty" => "FSoftObjectPath",
                        "ObjectProperty" or "ClassProperty" => "UObject*",
                        "EnumProperty" => scriptArray.InnerTagData?.EnumName,
                        "StructProperty" => $"F{scriptArray.InnerTagData?.StructType}",
                        "InterfaceProperty" => $"F{scriptArray.InnerTagData?.InnerType}", // check
                        _ => $"Variable type of InnerType '{scriptArray.InnerType}' is currently not supported for UScriptArray"
                    };

                    type = $"TArray<{innerType}>";
                }
                else
                {
                    var customStringBuilder = new CustomStringBuilder();
                    customStringBuilder.OpenBlock();
                    foreach (var property in scriptArray.Properties)
                    {
                        if (!GetPropertyTagVariable(
                                new FPropertyTag(new FName(scriptArray.InnerType), property, scriptArray.InnerTagData),
                                out type, out var innerValue))
                        {
                            Log.Warning("Failed to get ArrayElement of type {type}", scriptArray.InnerType);
                            continue;
                        }

                        customStringBuilder.AppendLine(innerValue);
                    }

                    customStringBuilder.CloseBlock();
                    type = $"TArray<{type}>";
                    value = customStringBuilder.ToString();
                }

                break;
            }
            case EPropertyType.StructProperty:
            {
                var structType = propertyTag.GetGenericValue<FScriptStruct>();
                if (!GetPropertyTagVariable(structType, out value))
                {
                    Log.Error("Unable to get struct value or type for FScriptStruct type {structType}",
                        structType.GetType().Name);
                    return false;
                }

                type = $"struct F{propertyTag.TagData?.StructType}";
                break;
            }
            case EPropertyType.StrProperty:
            {
                type = "FString";
                value = $"\"{propertyTag.GetGenericValue<string>()}\"";
                break;
            }
            case EPropertyType.MulticastDelegateProperty:
            {
                type = "FMulticastScriptDelegate";
                var list = propertyTag.GetGenericValue<FMulticastScriptDelegate>().InvocationList;

                if (list.Length == 0)
                {
                    value = "[]";
                }
                else
                {
                    var functions = string.Join(", ", list.Select(x => $"\"{x.FunctionName}\""));
                    value = $"[{functions}]";
                }

                break;
            }
            case EPropertyType.TextProperty:
            {
                var genericValue = propertyTag.GetGenericValue<FText>();

                var flags = genericValue.Flags.ToStringBitfield();
                //var historyText = genericValue.TextHistory.Text;
                var text = genericValue.Text;

                // TODO: find a way to show TextHistory?
                // none Base type ^^ as that's just text

                type = "FText";
                value = genericValue.Flags == 0
                    ? $"FText(\"{text}\")"
                    : $"FText(\"{text}\", {flags})";

                break;
            }
            case EPropertyType.AssetObjectProperty: // AssetObjectProperty is the old name of SoftObjectProperty
            {
                var softObjectPath = propertyTag.Tag.GenericValue;

                type = "FSoftObjectPath";
                value = $"FSoftObjectPath(\"{softObjectPath}\")";

                break;
            }
            case EPropertyType.SoftObjectProperty or EPropertyType.SoftClassProperty:
            {
                var softObjectPath = propertyTag.GetGenericValue<FSoftObjectPath>();

                type = "FSoftObjectPath";
                value = $"FSoftObjectPath(\"{softObjectPath.ToString()}\")";

                break;
            }
            case EPropertyType.UInt64Property:
            {
                type = "uint64";
                value = propertyTag.GetGenericValue<ulong>().ToString();
                break;
            }
            case EPropertyType.UInt32Property:
            {
                type = "uint32";
                value = propertyTag.GetGenericValue<uint>().ToString();
                break;
            }
            case EPropertyType.UInt16Property:
            {
                type = "uint16";
                value = propertyTag.GetGenericValue<ushort>().ToString();
                break;
            }
            case EPropertyType.Int64Property:
            {
                type = "int64";
                value = propertyTag.GetGenericValue<long>().ToString();
                break;
            }
            case EPropertyType.Int16Property:
            {
                type = "int16";
                value = propertyTag.GetGenericValue<short>().ToString();
                break;
            }
            case EPropertyType.Int8Property:
            {
                type = "int8";
                value = propertyTag.GetGenericValue<sbyte>().ToString();
                break;
            }
            case EPropertyType.MapProperty:
            {
                var scriptMap = propertyTag.GetGenericValue<UScriptMap>();

                if (scriptMap == null || scriptMap.Properties == null)
                {
                    value = "{}";
                    type = "TMap<unknown, unknown>";
                    break;
                }

                if (scriptMap.Properties.Count > 0)
                {
                    var keyType = string.Empty;
                    var valueType = string.Empty;

                    var customStringBuilder = new CustomStringBuilder();
                    var keyValueList = new List<string>(scriptMap.Properties.Count);

                    customStringBuilder.OpenBlock();
                    foreach (var (mapKey, mapValue) in scriptMap.Properties)
                    {
                        var innerTypeData = propertyTag.TagData?.InnerTypeData;
                        var keyProperty = new FPropertyTag(new FName(innerTypeData?.Type), mapKey, innerTypeData);

                        if (!GetPropertyTagVariable(keyProperty, out keyType, out var keyValue))
                        {
                            Log.Warning("Unable to get KeyValue for UScriptMap of type: {type}", mapKey.GetType().Name);
                            continue;
                        }

                        var valueTypeData = propertyTag.TagData?.ValueTypeData;
                        var valueProperty = new FPropertyTag(new FName(valueTypeData?.Type), mapValue, valueTypeData);

                        if (!GetPropertyTagVariable(valueProperty, out valueType, out var valueValue))
                        {
                            Log.Warning("Unable to get MapValue for UScriptMap of type: {type}",
                                mapValue.GetType().Name);
                        }

                        keyValueList.Add($"{{ {keyValue}, {valueValue} }}");
                    }

                    var keyValueString = string.Join(", \n", keyValueList);

                    customStringBuilder.AppendLine($"{keyValueString}");
                    customStringBuilder.CloseBlock();

                    type = $"TMap<{keyType}, {valueType}>";
                    value = customStringBuilder.ToString();
                }
                else
                {
                    string GetScriptArrayTypes(FPropertyTagData? tagType)
                    {
                        return tagType?.Type switch
                        {
                            "EnumProperty" or "ByteProperty" when tagType.EnumName != null => tagType.EnumName,
                            "IntProperty" => "int",
                            "Int8Property" => "int8",
                            "Int16Property" => "int16",
                            "Int64Property" => "int64",
                            "UInt16Property" => "uint16",
                            "UInt32Property" => "uint32",
                            "UInt64Property" => "uint64",
                            "ByteProperty" => "byte",
                            "BoolProperty" => "bool",
                            "StrProperty" => "string",
                            "DoubleProperty" => "double",
                            "NameProperty" => "FName",
                            "TextProperty" => "FText",
                            "FloatProperty" => "float",
                            "SoftObjectProperty" or "AssetObjectProperty" => "FSoftObjectPath",
                            "ObjectProperty" or "ClassProperty" => "UObject*",
                            "StructProperty" => $"F{tagType.StructType}",
                            "InterfaceProperty" => $"F{tagType.StructType}", // check
                            _ => throw new NotSupportedException(
                                $"PropertyType {tagType?.Type} is currently not supported")
                        };
                    }

                    var keyType = GetScriptArrayTypes(propertyTag.TagData?.InnerTypeData);
                    var valueType = GetScriptArrayTypes(propertyTag.TagData?.ValueTypeData);


                    type = $"TMap<{keyType}, {valueType}>";
                    value = "{}";
                }

                break;
            }
            case EPropertyType.EnumProperty:
            {
                value = propertyTag.GetGenericValue<FName>().ToString();
                type = $"enum {propertyTag.TagData?.EnumName}";
                break;
            }
            case EPropertyType.FieldPathProperty:
            {
                value = $"\"{propertyTag.GetGenericValue<FFieldPath>().ToString() ?? string.Empty}\"";
                type = "FieldPath";
                break;
            }
            // todo
            case EPropertyType.WeakObjectProperty:
            {
                type = "WeakObject";
                return true;
            }
            case EPropertyType.InterfaceProperty:
            {
                type = "Interface";
                return true;
            }
            case EPropertyType.OptionalProperty:
            {
                type = $"TOptional<{propertyTag.TagData?.InnerType}>";
                return true;
            }
            case EPropertyType.SetProperty:
            {
                type = "TArray";
                return true;
            }
            case EPropertyType.DelegateProperty:
            {
                type = "Delegate";
                return true;
            }
            case EPropertyType.MulticastInlineDelegateProperty:
            {
                // TODO: this motherfucker
                type = "FMulticastScriptDelegate";
                return true;
            }
            default:
            {
                Log.Warning($"EPropertyType {propertyTag.TagData?.Type} is currently not implemented");
                return false;
            }
        }

        return !string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(value);
    }

    private static bool GetPropertyTagVariable(FScriptStruct scriptStruct, out string value) =>
        GetPropertyTagVariable(scriptStruct.StructType, out value);

    private static bool GetPropertyTagVariable(IUStruct uStruct, out string value)
    {
        value = string.Empty;

        switch (uStruct)
        {
            case FStructFallback fallback:
            {
                if (fallback.Properties.Count == 0)
                {
                    value = "{}";
                }
                else
                {
                    var stringBuilder = new CustomStringBuilder();
                    stringBuilder.OpenBlock();
                    for (int i = 0; i < fallback.Properties.Count; i++)
                    {
                        var property = fallback.Properties[i];
                        GetPropertyTagVariable(property, out string _, out string tagValue);
                        bool isLast = i == fallback.Properties.Count - 1;
                        stringBuilder.AppendLine($"\"{property.Name}\": {tagValue}{(isLast ? "" : ",")}");
                    }

                    stringBuilder.CloseBlock();

                    value = stringBuilder.ToString();
                }

                break;
            }
            case FVector vector:
            {
                var x = vector.X;
                var y = vector.Y;
                var z = vector.Z;

                value = $"FVector({x}, {y}, {z})";
                break;
            }
            case FGuid guid:
            {
                var a = $"0x{guid.A:X8}";
                var b = $"0x{guid.B:X8}";
                var c = $"0x{guid.C:X8}";
                var d = $"0x{guid.D:X8}";

                value = $"FGuid({a}, {b}, {c}, {d})";
                break;
            }
            case FVector4 vector4:
            {
                var x = vector4.X;
                var y = vector4.Y;
                var z = vector4.Z;
                var w = vector4.W;

                value = $"FVector4({x}, {y}, {z}, {w})";
                break;
            }
            case TIntVector2<float> floatVector2:
            {
                var x = floatVector2.X;
                var y = floatVector2.Y;
                value = $"TIntVector2<float>({x}, {y})";
                break;
            }
            case TIntVector3<float> floatVector3:
            {
                var x = floatVector3.X;
                var y = floatVector3.Y;
                var z = floatVector3.Z;
                value = $"TIntVector3<float>({x}, {y}, {z})";
                break;
            }
            case TIntVector4<float> floatVector3:
            {
                var x = floatVector3.X;
                var y = floatVector3.Y;
                var z = floatVector3.Z;
                value = $"TIntVector4<float>({x}, {y}, {z})";
                break;
            }
            case FVector2D vector2d:
            {
                var x = vector2d.X;
                var y = vector2d.Y;

                value = $"FVector2D({x}, {y})";
                break;
            }
            case FQuat fQuat:
            {
                var x = fQuat.X;
                var y = fQuat.Y;
                var z = fQuat.Z;
                var w = fQuat.W;

                value = $"FQuat({x}, {y}, {z}, {w})";
                break;
            }
            case FBox box:
            {
                GetPropertyTagVariable(box.Min, out var min);
                GetPropertyTagVariable(box.Max, out var max);
                var isValid = box.IsValid;

                value = $"FBox({min}, {max}, {isValid})";
                break;
            }
            case TBox2<FVector2D> box2D:
            {
                GetPropertyTagVariable(box2D.Min, out var min);
                GetPropertyTagVariable(box2D.Max, out var max);
                var isValid = box2D.bIsValid;

                value = $"FBox2D({min}, {max}, {isValid})";

                break;
            }
            case FRotator rotator:
            {
                var pitch = rotator.Pitch;
                var yaw = rotator.Yaw;
                var roll = rotator.Roll;

                value = $"FRotator({pitch}, {yaw}, {roll})";
                break;
            }
            case FLinearColor linearColor:
            {
                var r = linearColor.R;
                var g = linearColor.G;
                var b = linearColor.B;
                var a = linearColor.A;

                value = $"FLinearColor({r}, {g}, {b}, {a})";
                break;
            }
            case FUniqueNetIdRepl netId:
            {
                var id = netId.UniqueNetId;
                value = $"FUniqueNetIdRepl({id})";
                break;
            }
            case FNavAgentSelector agent:
            {
                var bits = agent.PackedBits;
                value = $"FNavAgentSelector({bits})";
                break;
            }
            case FGameplayTagContainer gameplayTagContainer:
            {
                var gameplayTagsList = new List<string>(gameplayTagContainer.GameplayTags.Length);
                foreach (var gameplayTag in gameplayTagContainer.GameplayTags)
                {
                    gameplayTagsList.Add($"FGameplayTag::RequestGameplayTag(FName(\"{gameplayTag.TagName.ToString()}\"))");
                }

                var customStringBuilder = new CustomStringBuilder();

                if (gameplayTagsList.Count == 0)
                {
                    customStringBuilder.Append("FGameplayTagContainer({})");
                }
                else
                {
                    var gameplayTags = string.Join(",\n", gameplayTagsList);
                    customStringBuilder.OpenBlock("FGameplayTagContainer({");
                    customStringBuilder.AppendLine(gameplayTags);
                    customStringBuilder.CloseBlock("})");
                }

                value = customStringBuilder.ToString();

                break;
            }
            case FDateTime dateTime:
            {
                value = $"FDateTime({dateTime})";
                break;
            }
            case FSoftObjectPath softObjectPath:
            {
                value = $"FSoftObjectPath(\"{softObjectPath.ToString()}\")";
                break;
            }
            case FColor color:
            {
                var r = color.B;
                var g = color.G;
                var b = color.B;
                var a = color.A;

                value = $"FColor({r}, {g}, {b}, {a})";

                break;
            }
            case FRichCurveKey richCurve:
            {
                var InterpMode = richCurve.InterpMode;
                var TangentMode = richCurve.TangentMode;
                var TangentWeightMode = richCurve.TangentWeightMode;
                var Time = richCurve.Time;
                var Value = richCurve.Value;
                var ArriveTangent = richCurve.ArriveTangent;
                var ArriveTangentWeight = richCurve.ArriveTangentWeight;
                var LeaveTangent = richCurve.LeaveTangent;
                var LeaveTangentWeight = richCurve.LeaveTangentWeight;

                value = $"FRichCurveKey({InterpMode}, {TangentMode}, {TangentWeightMode}, {Time}, {Value}, {ArriveTangent}, {ArriveTangentWeight}, {LeaveTangent}, {LeaveTangentWeight})";

                break;
            }
            default:
            {
                value = uStruct.ToString() ?? string.Empty; // real
                Log.Warning("Property Type '{type}' is currently not supported for FScriptStruct", uStruct.GetType().Name);
                break;
            }
        }

        return !string.IsNullOrWhiteSpace(value);
    }

    public static string GetLineExpression(KismetExpression kismetExpression)
    {
        // TODO: Everything that include Const will have the const keyword at the start **maybe**
        // what is this comment, there is no flag const for expressions?
        switch (kismetExpression)
        {
            // what's the difference between localVariable, instanceVariable and EX_DefaultVariable
            case EX_LocalVariable localVariable:
            {
                return localVariable.Variable.ToString();
            }
            case EX_InstanceVariable instanceVariable:
            {
                return instanceVariable.Variable.ToString();
            }
            case EX_DefaultVariable defaultVariable:
            {
                return defaultVariable.Variable.ToString();
            }
            case EX_LocalOutVariable localOutVariable:
            {
                return $"{localOutVariable.Variable}"; // it is & but makes no sense tbh
            }
            case EX_LetValueOnPersistentFrame persistent:
            {
                var variableAssignment = GetLineExpression(persistent.AssignmentExpression);
                var variableToBeAssigned = persistent.DestinationProperty.ToString();

                return $"{(variableToBeAssigned.Contains("K2Node_") ? "UberGraphFrame->" + variableToBeAssigned : variableToBeAssigned)} = {variableAssignment}";
            }
            case EX_Let let:
            {
                var assignment = GetLineExpression(let.Assignment);
                var variable = GetLineExpression(let.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_LetMulticastDelegate letMulticastDelegate: // idk
            {
                var assignment = GetLineExpression(letMulticastDelegate.Assignment);
                var variable = GetLineExpression(letMulticastDelegate.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_LetBool letBool:
            {
                var variable = GetLineExpression(letBool.Variable);
                var assignment = GetLineExpression(letBool.Assignment);

                return $"{variable} = {assignment}";
            }
            case EX_Context_FailSilent failSilent:
            {
                var function = failSilent?.ContextExpression is not null ? GetLineExpression(failSilent?.ContextExpression).SubstringAfter("::") : "failedplaceholder";
                var obj = failSilent?.ObjectExpression is not null ? GetLineExpression(failSilent?.ObjectExpression) : "failedplaceholder";

                var customStringBuilder = new CustomStringBuilder();

                customStringBuilder.AppendLine($"if ({obj})");
                customStringBuilder.IncreaseIndentation();
                customStringBuilder.Append($"{obj}->{function}");

                return customStringBuilder.ToString();
            }
            case EX_Context context:
            {
                var function = context?.ContextExpression is not null ? GetLineExpression(context?.ContextExpression).SubstringAfter("::") : "failedplaceholder";
                var obj = context?.ObjectExpression is not null ? GetLineExpression(context?.ObjectExpression) : "failedplaceholder";

                return $"{obj}->{function}";
            }
            case EX_CallMath callMath:
            {
                var parametersList = new List<string>(callMath.Parameters.Length);
                foreach (var parameter in callMath.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                var pkgIndex = callMath.StackNode.ToString();

                var functionName = pkgIndex.SubstringAfter(':').Trim('\'');
                var classType = pkgIndex.SubstringBefore(':').Trim();
                var className = classType.Split('.').LastOrDefault();

                return $"{className}::{functionName}({parameters})";
            }
            case EX_LocalVirtualFunction localVirtualFunction:
            {
                var parametersList = new List<string>(localVirtualFunction.Parameters.Length);
                foreach (var parameter in localVirtualFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                var functionName = localVirtualFunction.VirtualFunctionName.ToString();
                return $"this->{functionName}({parameters})"; // sometimes "this->" is wrong
            }
            case EX_LocalFinalFunction localFinalFunction:
            {
                var parametersList = new List<string>(localFinalFunction.Parameters.Length);
                foreach (var parameter in localFinalFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                var stackNode = localFinalFunction.StackNode.ToString();
                var functionName = stackNode.SubstringAfter(':').SubstringBefore("'");
                return $"{functionName}({parameters})";
            }
            case EX_VirtualFunction virtualFunction:
            {
                var parametersList = new List<string>(virtualFunction.Parameters.Length);
                foreach (var parameter in virtualFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                return $"{virtualFunction.VirtualFunctionName.ToString()}({parameters})";
            }
            case EX_TextConst textConst:
            {
                if (textConst.Value is FScriptText scriptText)
                {
                    return scriptText.SourceString is null
                        ? "nullptr"
                        : GetLineExpression(scriptText.SourceString);
                }

                return textConst.Value?.ToString() ?? "nullptr";
            }
            case EX_FinalFunction finalFunction:
            {
                var parametersList = new List<string>(finalFunction.Parameters.Length);
                foreach (var parameter in finalFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                var pkgIndex = finalFunction.StackNode.ToString();

                var classType = $"{pkgIndex.SubstringAfter('.').SubstringBefore(':')}";
                var functionName = pkgIndex.SubstringAfter(':').SubstringBefore("'");

                return $"{classType}::{functionName}({parameters})";
            }
            case EX_SetSet setSet:
            {
                var target = GetLineExpression(setSet.SetProperty);

                if (setSet.Elements.Length == 0)
                {
                    return $"{target} = TArray {{ }};";
                }

                var values = new List<string>(setSet.Elements.Length);
                foreach (var element in setSet.Elements)
                {
                    values.Add(GetLineExpression(element));
                }

                var joined = string.Join(", ", values);
                return $"{target} = TArray {{ {joined} }};";
            }
            case EX_SetConst setConst:
            {
                if (setConst.Elements.Length == 0)
                {
                    return "TArray { };";
                }

                var values = new List<string>(setConst.Elements.Length);
                foreach (var element in setConst.Elements)
                {
                    values.Add(GetLineExpression(element));
                }

                var joined = string.Join(", ", values);
                return $"TArray {{ {joined} }};";
            }
            case EX_ArrayConst constArray:
            {
                if (constArray.Elements.Length > 0)
                {
                    var values = new List<string>(constArray.Elements.Length);
                    foreach (var element in constArray.Elements)
                    {
                        values.Add(GetLineExpression(element));
                    }

                    return $"TArray<>({string.Join(", ", values)})"; // todo type
                }
                else
                {
                    return "TArray<>([])"; // todo type
                }
            }
            case EX_SetArray setArray:
            {
                var variable = GetLineExpression(setArray.AssigningProperty);

                string value = setArray.Elements.Length > 0
                    ? string.Join(", ", setArray.Elements.Select(element => GetLineExpression(element)))
                    : "[]";

                return $"{variable} = {value}";
            }
            case EX_IntConst intConst:
            {
                return intConst.Value.ToString();
            }
            case EX_ByteConst:
            case EX_IntConstByte:
            {
                var byteConst = (EX_ByteConst)kismetExpression;
                return $"0x{byteConst.Value:X}";
            }
            case EX_ObjectConst objectConst:
            {
                var pkgIndex = objectConst.Value.ToString();

                if (!string.IsNullOrEmpty(pkgIndex) && pkgIndex.Contains('\''))
                {
                    var parts = pkgIndex.Split('\'');
                    var typeName = parts[0];
                    var path = parts[1];

                    var classpkgType = $"U{typeName}";
                    return $"FindObject<{classpkgType}>(nullptr, \"{path}\")";
                }

                return $"FindObject<UObject>(nullptr, \"{pkgIndex}\")";
            }
            case EX_NameConst nameConst:
            {
                return $"\"{nameConst.Value.ToString()}\"";
            }
            case EX_Vector3fConst vec3:
            {
                var value = vec3.Value;
                return $"FVector3f({value.X}, {value.Y}, {value.Z})";
            }
            case EX_TransformConst xf:
            {
                var value = xf.Value;
                return
                    $"FTransform(FQuat({value.Rotation.X}, {value.Rotation.Y}, {value.Rotation.Z}, {value.Rotation.W}), FVector({value.Translation.X}, {value.Translation.Y}, {value.Translation.Z}), FVector({value.Scale3D.X}, {value.Scale3D.Y}, {value.Scale3D.Z}))";
            }
            case EX_Int64Const i64:
            {
                return i64.Value.ToString();
            }
            case EX_UInt64Const ui64:
            {
                return ui64.Value.ToString();
            }
            case EX_BitFieldConst bit:
            {
                return bit.ConstValue.ToString();
            }
            case EX_UnicodeStringConst uni:
            {
                return $"\"{uni.Value}\"";
            }
            case EX_StringConst stringConst:
            {
                return $"\"{stringConst.Value}\"";
            }
            case EX_InstanceDelegate del:
            {
                return $"\"{del.FunctionName}\"";
            }
            case EX_IntOne:
            {
                return "1";
            }
            case EX_IntZero:
            {
                return "0";
            }
            case EX_True:
            {
                return "true";
            }
            case EX_False:
            {
                return "false";
            }
            case EX_Self:
            {
                return "this";
            }
            case EX_Cast cast:
            {
                var target = GetLineExpression(cast.Target);
                var conversionType = cast.ConversionType switch
                {
                    ECastToken.CST_ObjectToBool or ECastToken.CST_ObjectToBool2 or ECastToken.CST_InterfaceToBool
                        or ECastToken.CST_InterfaceToBool2 => "bool",
                    ECastToken.CST_DoubleToFloat => "float",
                    ECastToken.CST_FloatToDouble => "double",
                    ECastToken.CST_ObjectToInterface => "Interface", // make sure this makes sense
                    _ => throw new NotImplementedException(
                        $"ConversionType {cast.ConversionType} is currently not implemented") // impossible
                };

                return $"Cast<{conversionType}>({target})";
            }
            case EX_PopExecutionFlowIfNot popExecutionFlowIfNot:
            {
                var booleanExpression = GetLineExpression(popExecutionFlowIfNot.BooleanExpression);
                var customStringBuilder = new CustomStringBuilder();

                customStringBuilder.AppendLine($"if (!{booleanExpression})");
                customStringBuilder.IncreaseIndentation();
                customStringBuilder.Append("return");

                return customStringBuilder.ToString();
            }
            case EX_JumpIfNot jumpIfNot:
            {
                var booleanExpression = GetLineExpression(jumpIfNot.BooleanExpression);
                var customStringBuilder = new CustomStringBuilder();

                customStringBuilder.AppendLine($"if (!{booleanExpression})");
                customStringBuilder.IncreaseIndentation();
                customStringBuilder.Append($"goto Label_{jumpIfNot.CodeOffset}");

                return customStringBuilder.ToString();
            }
            case EX_SkipOffsetConst skipOffsetConst:
            {
                return $"goto Label_{skipOffsetConst.Value}";
            }
            case EX_Jump jump:
            {
                return $"goto Label_{jump.CodeOffset}";
            }
            case EX_ComputedJump computedJump:
            {
                if (computedJump.CodeOffsetExpression is EX_VariableBase)
                {
                    return $"goto {GetLineExpression(computedJump.CodeOffsetExpression)}";
                }

                return GetLineExpression((EX_CallMath)computedJump.CodeOffsetExpression);
            }

            case EX_LetWeakObjPtr letWeakObj: // what's the difference? let obj, let weak obj and others?
            {
                var assignment = GetLineExpression(letWeakObj.Assignment);
                var variable = GetLineExpression(letWeakObj.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_LetDelegate letDelegate: // comment above
            {
                var assignment = GetLineExpression(letDelegate.Assignment);
                var variable = GetLineExpression(letDelegate.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_LetObj letObj:
            {
                var assignment = GetLineExpression(letObj.Assignment);
                var variable = GetLineExpression(letObj.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_ArrayGetByRef arrayRef:
            {
                var arrayIndex = GetLineExpression(arrayRef.ArrayIndex);
                var arrayVariable = GetLineExpression(arrayRef.ArrayVariable);

                return $"{arrayVariable}[{arrayIndex}]";
            }
            case EX_InterfaceContext interfaceContext:
            {
                return GetLineExpression(interfaceContext.InterfaceValue);
            }
            case EX_NoInterface:
            case EX_NoObject:
            {
                return "nullptr";
            }
            case EX_Return returnExpr:
            {
                var returnValue = GetLineExpression(returnExpr.ReturnExpression);
                if (returnExpr.ReturnExpression.Token == EExprToken.EX_Nothing)
                {
                    return "return";
                }

                return $"return {returnValue}";
            }
            case EX_SoftObjectConst objectConst:
            {
                var stringValue = GetLineExpression(objectConst.Value);
                return $"FSoftObjectPath({stringValue}";
            }

            case EX_MetaCast:
            case EX_DynamicCast:
            case EX_CrossInterfaceCast:
            case EX_ObjToInterfaceCast:
            case EX_InterfaceToObjCast:
            {
                var cast = (EX_CastBase)kismetExpression;
                var variable = GetLineExpression(cast.Target);
                var classType = cast.ClassPtr.Name;

                string castFunc;

                switch (kismetExpression.Token)
                {
                    case EExprToken.EX_MetaCast:
                        castFunc = $"CastClass<{classType}>";
                        break;
                    case EExprToken.EX_DynamicCast:
                    case EExprToken.EX_CrossInterfaceCast:
                    case EExprToken.EX_InterfaceToObjCast:
                        castFunc = $"Cast<{classType}>";
                        break;
                    case EExprToken.EX_ObjToInterfaceCast:
                    {
                        var structCast = $"I{cast.ClassPtr.ToString().SubstringBeforeLast("'").SubstringAfter('.')}";
                        return $"Cast<{classType}*>({structCast})";
                    }
                    default:
                        castFunc = $"Cast<{classType}>";
                        break;
                }

                return $"{castFunc}({variable})";
            }
            case EX_BindDelegate bindDelegate:
            {
                var delegateVar = GetLineExpression(bindDelegate.Delegate);
                var objectTerm = GetLineExpression(bindDelegate.ObjectTerm);
                var functionName = $"FName(\"{bindDelegate.FunctionName.ToString()}\")";

                return $"{delegateVar}->BindUFunction({objectTerm}, {functionName})";
            }
            case EX_StructConst structConst:
            {
                var structName = $"F{structConst.Struct.Name}";

                var properties = new List<string>(structConst.Properties.Length);
                foreach (var property in structConst.Properties)
                {
                    properties.Add(GetLineExpression(property));
                }

                var parameters = string.Join(", ", properties);
                return $"{structName}({parameters})";
            }
            case EX_FloatConst floatConst:
            {
                return floatConst.Value.ToString(CultureInfo.CurrentCulture);
            }
            case EX_AddMulticastDelegate multicastDelegate:
            {
                var delegatee = GetLineExpression(multicastDelegate.Delegate);
                var delegateToAdd = GetLineExpression(multicastDelegate.DelegateToAdd);

                return $"{delegatee}->Add({delegateToAdd})";
            }
            case EX_RotationConst rotationConst:
            {
                var pitch = rotationConst.Value.Pitch;
                var roll = rotationConst.Value.Roll;
                var yaw = rotationConst.Value.Yaw;

                return $"FRotator({pitch}, {roll}, {yaw})";
            }
            case EX_SetMap setMap:
            {
                var target = GetLineExpression(setMap.MapProperty);

                if (setMap.Elements.Length == 0)
                {
                    return $"{target} = TMap {{ }}";
                }

                //FortniteGame/Content/UI/InGame/HUD/WBP_QuickEditGrid.uasset
                //"ValueProp": {
                //    "Type": "ObjectProperty", add type <>
                var stringBuilder = new CustomStringBuilder();
                stringBuilder.Append($"{target} = TMap {{ ");

                for (int i = 0; i < setMap.Elements.Length; i++)
                {
                    var element = setMap.Elements[i];
                    var elementText = GetLineExpression(element);

                    stringBuilder.Append(elementText);

                    bool isLast = (i == setMap.Elements.Length - 1);
                    if (!isLast)
                    {
                        stringBuilder.Append(element.Token == EExprToken.EX_InstanceVariable ? ": " : ", ");
                    }
                }

                stringBuilder.Append(" }");
                return stringBuilder.ToString();
            }
            case EX_MapConst mapConst:
            {
                if (mapConst.Elements.Length == 0)
                {
                    return "TMap { }";
                }

                var stringBuilder = new CustomStringBuilder();
                stringBuilder.Append("TMap { ");

                for (int i = 0; i < mapConst.Elements.Length; i++)
                {
                    var element = mapConst.Elements[i];
                    var elementText = GetLineExpression(element);

                    stringBuilder.Append(elementText);

                    bool isLast = (i == mapConst.Elements.Length - 1);
                    if (!isLast)
                    {
                        stringBuilder.Append(element.Token == EExprToken.EX_InstanceVariable ? ": " : ", ");
                    }
                }

                stringBuilder.Append(" }");
                return stringBuilder.ToString();
            }
            case EX_SwitchValue switchValue:
            {
                if (switchValue.Cases.Length == 2)
                {
                    var indexTerm = GetLineExpression(switchValue.IndexTerm);

                    var case1 = GetLineExpression(switchValue.Cases[0].CaseIndexValueTerm);
                    var case2 = GetLineExpression(switchValue.Cases[1].CaseIndexValueTerm);

                    return $"{indexTerm} ? {case1} : {case2}";
                }

                var stringBuilder = new CustomStringBuilder();

                stringBuilder.AppendLine($"switch ({GetLineExpression(switchValue.IndexTerm)})");
                stringBuilder.OpenBlock();

                foreach (var caseItem in switchValue.Cases)
                {
                    string caseLabel;
                    if (caseItem.CaseIndexValueTerm.Token == EExprToken.EX_IntConst)
                    {
                        caseLabel = ((EX_IntConst)caseItem.CaseIndexValueTerm).Value.ToString();
                    }
                    else
                    {
                        caseLabel = GetLineExpression(caseItem.CaseIndexValueTerm);
                    }

                    stringBuilder.AppendLine($"case {caseLabel}:");
                    stringBuilder.OpenBlock();

                    stringBuilder.AppendLine($"return {GetLineExpression(caseItem.CaseTerm)};");
                    stringBuilder.AppendLine("break;");

                    stringBuilder.CloseBlock("}\n");
                }

                stringBuilder.AppendLine("default:");
                stringBuilder.OpenBlock();

                stringBuilder.AppendLine($"return {GetLineExpression(switchValue.DefaultTerm)};");
                stringBuilder.AppendLine("break;");

                stringBuilder.CloseBlock("}\n");

                stringBuilder.CloseBlock();

                return stringBuilder.ToString();
            }
            case EX_DoubleConst doubleConst:
            {
                return doubleConst.Value.ToString(CultureInfo.CurrentCulture);
            }
            case EX_StructMemberContext structMemberContext:
            {
                if (structMemberContext.Property.New?.Path.Count > 1)
                    throw new NotImplementedException();

                var property = structMemberContext.Property.ToString();
                var structExpression = GetLineExpression(structMemberContext.StructExpression);

                return $"{structExpression}.{property}";
            }
            case EX_CallMulticastDelegate callMulticastDelegate:
            {
                var callDelegate = GetLineExpression(callMulticastDelegate.Delegate);

                var parameters = new List<string>(callMulticastDelegate.Parameters.Length);
                foreach (var parameter in callMulticastDelegate.Parameters)
                {
                    parameters.Add(GetLineExpression(parameter));
                }

                var parametersString = string.Join(", ", parameters);
                //var functionName = callMulticastDelegate.StackNode.Name; // TODO: show the functionName somehow, maybe with comments added "// {funcName}"

                return $"{callDelegate}->Broadcast({parametersString})";
            }
            case EX_RemoveMulticastDelegate removeMulticastDelegate:
            {
                var delegateExpr = removeMulticastDelegate.Delegate;
                var delegateTarget = GetLineExpression(delegateExpr);
                var delegateToRemove = GetLineExpression(removeMulticastDelegate.DelegateToAdd);

                var separator = delegateExpr.Token == EExprToken.EX_Context ? "->" : ".";

                return $"{delegateTarget}{separator}RemoveDelegate({delegateToRemove});";
            }
            case EX_ClearMulticastDelegate clearMulticastDelegate:
            {
                var delegateTarget = GetLineExpression(clearMulticastDelegate.DelegateToClear);
                return $"{delegateTarget}.Clear();";
            }
            // todo: are these three correct?
            case EX_PropertyConst propertyConst:
            {
                return propertyConst.Property.ToString();
            }
            case EX_ClassSparseDataVariable classSparse:
            {
                return classSparse.Variable.ToString();
            }
            case EX_FieldPathConst fieldPathConst:
            {
                var value = GetLineExpression(fieldPathConst.Value);
                return value;
            }

            // good enough?
            case EX_WireTracepoint:
            case EX_Tracepoint:
            {
#if DEBUG
                return "throw std::runtime_error(\"TracePoint hit\");";
#endif
                return "";
            }
            case EX_Breakpoint:
            {
#if DEBUG
                return "breakpoint;";
#endif
                return "";
            }
            case EX_VectorConst vectorConst:
            {
                var x = vectorConst.Value.X;
                var y = vectorConst.Value.Y;
                var z = vectorConst.Value.Z;

                return $"FVector({x}, {y}, {z})";
            }
            case EX_Nothing:
            case EX_NothingInt32:
            case EX_EndFunctionParms:
            case EX_EndStructConst:
            case EX_EndArray:
            case EX_EndArrayConst:
            case EX_EndSet:
            case EX_EndMap:
            case EX_EndMapConst:
            case EX_EndSetConst:
            case EX_PushExecutionFlow:
            case EX_PopExecutionFlow:
            case EX_AutoRtfmStopTransact:
            case EX_AutoRtfmTransact:
            case EX_AutoRtfmAbortIfNot:
            {
                // added as sometimes it's throwing not supported
                return "";
            }

            /*
                EExprToken.EX_Assert
                EExprToken.EX_Skip
                EExprToken.EX_InstrumentationEvent
                EExprToken.EX_FieldPathConst
                EExprToken.EX_ClassContext it's like EX_Context
            */
            default:
                throw new NotImplementedException($"KismetExpression '{kismetExpression.GetType().Name}' is currently not supported");
        }
    }
}