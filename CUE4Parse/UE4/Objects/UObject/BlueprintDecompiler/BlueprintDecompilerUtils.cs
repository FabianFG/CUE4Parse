using System.Globalization;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;

public static class BlueprintDecompilerUtils
{
    public static string GetClassWithPrefix(UStruct? struc)
    {
        var prefix = GetPrefix(struc);
        return $"{prefix}{struc?.Name}";
    }

    private static string GetPrefix(UStruct? struc)
    {
        while (true)
        {
            var structName = struc?.Name;
            var prefix = structName switch
            {
                "Actor" => "A",
                "Interface" => "I",
                "Object" => "U",
                _ => null
            };

            if (!string.IsNullOrEmpty(prefix))
                return prefix;

            if (struc?.SuperStruct is null || !struc.SuperStruct.TryLoad(out struc))
                break;
        }

        return "U";
    }

    public static string? GetPropertyTagType(FPropertyTag propertyType)
    {
        string? text = null;
        switch (propertyType.TagData?.Type)
        {
            case "StructProperty":
            {
                text = $"struct F{propertyType.TagData.StructType}";
                break;
            }
            case "MapProperty":
            {
                text = $"struct TMap<{propertyType.TagData.StructType}>";
                break;
            }
            case "ObjectProperty":
            case "ClassProperty": // todo: is ClassProperty correct?
            {
                var classType = (propertyType.Tag?.GenericValue as FPackageIndex)?.ResolvedObject?.Class?.Name;
                text = $"class U{classType}*";
                break;
            }
            case "ArrayProperty":
            {
                var scriptArray = propertyType.Tag?.GenericValue as UScriptArray;
                if (scriptArray!.Properties.Count <= 0)
                {
                    Log.Warning("ScriptArray properties count is less than one. Properties Count: {count}", scriptArray.Properties.Count);
                    break;
                }

                var scriptArrayType = GetPropertyTagType(scriptArray.Properties[0]);
                text = $"TArray<{scriptArrayType}>";

                break;
            }
            case "IntProperty":
            {
                text = "int";
                break;
            }
            case "DoubleProperty":
            {
                text = "double";
                break;
            }
            case "BoolProperty":
            {
                text = "bool";
                break;
            }
            case "EnumProperty":
            {
                text = propertyType.TagData.EnumName!;
                break;
            }
            default:
            {
                Log.Warning("Property Type '{type}' is currently not supported", propertyType.TagData?.Type);
                break;
            }
        }

        return text;
    }

    private static string? GetPropertyTagType(FPropertyTagType property)
    {
        string? text = null;

        switch (property)
        {
            case ObjectProperty objProperty:
            {
                var objectType = (objProperty.GenericValue as FPackageIndex)?.ResolvedObject?.Class?.Name;
                text = $"U{objectType}*";
                break;
            }
            default:
            {
                Log.Warning("Property Value '{type}' is currently not supported for UScriptArray", property.GetType().Name);
                break;
            }
        }

        return text;
    }

    public static bool IsPointer(FProperty property) => property.PropertyFlags.HasFlag(EPropertyFlags.ReferenceParm) ||
                                                        property.PropertyFlags.HasFlag(EPropertyFlags.InstancedReference) ||
                                                        property.PropertyFlags.HasFlag(EPropertyFlags.ContainsInstancedReference) ||
                                                        property.GetType() == typeof(FObjectProperty);

    public static (string?, string?) GetPropertyType(FProperty property)
    {
        string? type = null;
        string? value = null;

        var propertyFlags = property.PropertyFlags;
        // Log.Debug("Property Flags: {flag}", propertyFlags.ToStringBitfield());

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
                type += $"class U{classType}*";

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
                type = "bool";
                break;
            }
            case FTextProperty textProperty:
            {
                type = "todo"; // todo:
                break;
            }
            case FStrProperty strProperty:
            {
                type = "string";
                break;
            }
            case FNameProperty nameProperty:
            {
                type = "todo"; // todo:
                break;
            }
            case FDelegateProperty delegateProperty:
            {
                type = "todo"; // todo:
                break;
            }
            default:
            {
                Log.Warning("Property Value '{type}' is currently not supported", property.GetType().Name);
                break;
            }
        }

        if (IsPointer(property) && !type.EndsWith("*"))
            type += "*";

        if (propertyFlags.HasFlag(EPropertyFlags.OutParm) && !propertyFlags.HasFlag(EPropertyFlags.ReturnParm))
            type += "&";

        return (value, type);
    }

    public static string? GetPropertyText(FPropertyTag propertyTag)
    {
        var propertyValue = propertyTag.Tag?.GenericValue;
        return GetPropertyText(propertyValue!);
    }

    private static string? GetPropertyText(object value)
    {
        string? text = null;
        switch (value)
        {
            case UScriptArray scriptArray:
            {
                if (scriptArray.Properties.Count > 0)
                {
                    var stringBuilder = new CustomStringBuilder();
                    stringBuilder.OpenBlock("[");
                    foreach (var property in scriptArray.Properties)
                    {
                        stringBuilder.AppendLine(GetPropertyText(property.GenericValue!)!);
                    }
                    stringBuilder.CloseBlock("]");

                    text = stringBuilder.ToString();
                }
                else
                {
                    text = "[]";
                }

                break;
            }
            case UScriptMap scriptMap:
            {
                if (scriptMap.Properties.Count > 0)
                {
                    var stringBuilder = new CustomStringBuilder();
                    stringBuilder.OpenBlock("[");

                    foreach (var KeyValue in scriptMap.Properties)
                    {
                        var keyText = GetPropertyText(KeyValue.Key)!;
                        var valueText = GetPropertyText(KeyValue.Value)!;

                        stringBuilder.OpenBlock();
                        stringBuilder.AppendLine($"\"{keyText}\": \"{valueText}\"");
                        stringBuilder.CloseBlock("},");
                    }

                    stringBuilder.CloseBlock("]");

                    text = stringBuilder.ToString();
                }
                else
                {
                    text = "[]";
                }

                break;
            }
            case FPackageIndex packageIndex:
            {
                text = $"\"{packageIndex}\"";
                break;
            }
            case FScriptStruct scriptStruct:
            {
                text = GetPropertyText(scriptStruct);
                break;
            }
            case FName name:
            {
                text = name.ToString();
                break;
            }
            case string textString:
            {
                text = textString;
                break;
            }
            case bool boolean:
            {
                text = boolean.ToString().ToLowerInvariant();
                break;
            }
            case int int32:
            {
                text = int32.ToString();
                break;
            }
            case long int64:
            {
                text = int64.ToString();
                break;
            }
            case float single:
            {
                text = single.ToString();
                break;
            }
            case double float32:
            {
                text = float32.ToString(CultureInfo.CurrentCulture);
                break;
            }
            default:
            {
                Log.Warning("Property Value '{type}' is currently not supported", value.GetType().Name);
                break;
            }
        }

        return text;
    }

    private static string? GetPropertyText(FScriptStruct scriptStruct)
    {
        string? text = null;

        switch (scriptStruct.StructType)
        {
            case FStructFallback fallback:
            {
                if (fallback.Properties.Count == 0)
                    return "[]";

                var stringBuilder = new CustomStringBuilder();
                stringBuilder.OpenBlock("[");
                foreach (var property in fallback.Properties)
                {
                    stringBuilder.AppendLine(property.ToString());
                }
                stringBuilder.CloseBlock("]");

                text = stringBuilder.ToString();
                break;
            }
            default:
            {
                Log.Warning("Property Type '{type}' is currently not supported for FScriptStruct", scriptStruct.StructType.GetType());
                break;
            }
        }

        return text;
    }
}
