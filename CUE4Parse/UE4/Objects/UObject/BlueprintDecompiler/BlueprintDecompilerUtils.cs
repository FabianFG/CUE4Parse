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
    
    public static string? GetPropertyType(FPropertyTag propertyType)
    {
        string? text = null;
        switch (propertyType.TagData?.Type)
        {
            case "StructProperty":
            {
                text = $"F{propertyType.TagData.StructType}";
                break;
            }
            case "ObjectProperty":
            {
                var packageIndex = (propertyType.Tag?.GenericValue as FPackageIndex)?.ToString().SubstringBefore("'");
                text = $"TObjectPtr<U{packageIndex}>";
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

                var scriptArrayType = GetPropertyType(scriptArray.Properties[0]);
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

    private static string? GetPropertyType(FPropertyTagType property)
    {
        string? text = null;
        
        switch (property)
        {
            case ObjectProperty objProperty:
            {
                var objectType = (objProperty.GenericValue as FPackageIndex)?.ToString().SubstringBefore("'");
                text = $"TObjectPtr<U{objectType}>";
                break;
            }
            default:
            {
                Log.Warning("Property Value '{type}' is currently not supported", property.GetType());
                break;
            }
        }

        return text;
    }

    public static string? GetPropertyText(FPropertyTag propertyTag)
    {
        var propertyValue = propertyTag.Tag?.GenericValue;
        return GetPropertyText(propertyValue!);
    }
    
    private static string? GetPropertyText(object value, bool bAddSemicolon = true)
    {
        string? text = null;
        switch (value)
        {
            case UScriptArray scriptArray:
            {
                var stringBuilder = new CustomStringBuilder();
                stringBuilder.OpenBlock("[");
                foreach (var property in scriptArray.Properties)
                {
                    stringBuilder.AppendLine(GetPropertyText(property.GenericValue!, false)!);
                }
                stringBuilder.CloseBlock("]");
                
                text = stringBuilder.ToString();
                break;
            }
            case FPackageIndex packageIndex:
            {
                text = packageIndex.ToString();
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
            case bool boolean:
            {
                text = boolean.ToString().ToLower();
                break;
            }
            case int int32:
            {
                text = int32.ToString();
                break;
            }
            case double float32:
            {
                text = float32.ToString(CultureInfo.CurrentCulture);
                break;
            }
            default:
            {
                Log.Warning("Property Value '{type}' is currently not supported", value.GetType());
                break;
            }
        }

        if (bAddSemicolon)
            text += ';';
        
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
                Log.Warning("Property Type '{type}' is currently not supported", scriptStruct.StructType.GetType());
                break;
            }
        }
        
        return text;
    }
}