using System;
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

    public static string GetPrefix(UStruct? struc)
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
    
    public static string GetPropertyType(FPropertyTag propertyType)
    {
        string text;
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
                text = $"U{packageIndex}*";
                break;
            }
            case "ArrayProperty":
            {
                var genericValue = propertyType.Tag?.GenericValue;
                var s = GetPropertyType(genericValue as UScriptArray);
                text = $"TArray<{s}>";
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
                throw new NotSupportedException($"Type {propertyType} is currently not supported");
                break;
            }
        }

        return text;
    }

    // TODO: remove this fucking idiota
    private static string GetPropertyType(UScriptArray scriptArray)
    {
        if (scriptArray.Properties.Count <= 0)
        {
            Log.Warning("Properties count for UScriptArray is less than or equal to 0");
        }

        var property = scriptArray.Properties[0];
        var text = string.Empty;
        
        switch (property)
        {
            case ObjectProperty objProperty:
            {
                var packageIndex = (objProperty.GenericValue as FPackageIndex)?.ToString().SubstringBefore("'");
                text = $"U{packageIndex}*";
                break;
            }
        }

        return text;
    }

    public static string GetPropertyText(FPropertyTag propertyTag)
    {
        var propertyValue = propertyTag.Tag?.GenericValue;
        return GetPropertyText(propertyValue!);
    }
    
    public static string GetPropertyText(object value)
    {
        string text;
        switch (value)
        {
            case FScriptStruct scriptStruct:
            {
                text = GetPropertyText(scriptStruct);
                break;
            }
            case FPackageIndex packageIndex:
            {
                text = packageIndex.ToString();
                break;
            }
            case UScriptArray scriptArray:
            {
                var stringBuilder = new CustomStringBuilder();
                stringBuilder.OpenBlock("[");
                foreach (var property in scriptArray.Properties)
                {
                    stringBuilder.AppendLine(GetPropertyText(property.GenericValue!));
                }
                stringBuilder.CloseBlock("]");
                
                text = stringBuilder.ToString();
                break;
            }
            case bool boolean:
            {
                text = boolean.ToString().ToLower();
                break;
            }
            case FName name:
            {
                text = name.ToString();
                break;
            }
            default:
            {
                Log.Warning("Property Value '{type}' is currently not supported", value.GetType());
                throw new NotImplementedException();
                break;
            }
        }
        
        return text;
    }

    public static string GetPropertyText(FScriptStruct scriptStruct)
    {
        var text = string.Empty;

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
                throw new NotImplementedException();
                break;
            }
        }
        
        return text;
    }
}