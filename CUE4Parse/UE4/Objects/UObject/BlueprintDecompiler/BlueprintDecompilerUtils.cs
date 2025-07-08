using System;
using CUE4Parse.UE4.Assets.Objects;
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
    
    public static string GetPropertyPrefix(string propertyType)
    {
        return propertyType switch
        {
            "StructProperty" => "F",
            "ObjectProperty" => "U",
            "ArrayProperty" => "[]",
            "BoolProperty" => "",
            _ => throw new NotSupportedException($"Type {propertyType} is currently not supported")
        };
    }

    public static string GetPropertyText(FPropertyTag propertyTag)
    {
        var propertyValue = propertyTag.Tag?.GenericValue;
        return GetPropertyText(propertyValue!);
    }
    
    public static string GetPropertyText(object value)
    {
        var text = string.Empty;
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
            case Boolean boolean:
            {
                text = boolean.ToString().ToLower();
                break;
            }
            default:
            {
                Log.Warning("Property Type '{type}' is currently not supported", value.GetType());
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