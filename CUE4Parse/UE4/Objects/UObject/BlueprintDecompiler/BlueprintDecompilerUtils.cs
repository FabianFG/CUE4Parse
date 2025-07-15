using System;
using System.Collections.Generic;
using System.Globalization;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;

public static class BlueprintDecompilerUtils
{
    // TODO: at the end
    // public static Dictionary<string, string> Structs = [];
    // public static Dictionary<string, string> Enums = [];
    
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
    
    private static bool IsPointer(FProperty property) => property.PropertyFlags.HasFlag(EPropertyFlags.ReferenceParm) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags.InstancedReference) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags.ContainsInstancedReference) ||
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
            case FBoolProperty:
            {
                type = "bool";
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

        if (propertyFlags.HasFlag(EPropertyFlags.OutParm))
            type += "&";
        
        return (value, type);
    }
    
    private static T GetGenericValue<T>(this FPropertyTag propertyTag) => (T) propertyTag.Tag?.GenericValue!;
    public static bool GetPropertyTagVariable(FPropertyTag propertyTag, out string type, out string value)
    {
        type = string.Empty;
        value = string.Empty;

        if (!Enum.TryParse<EPropertyType>(propertyTag.PropertyType.ToString(), out var propertyType))
            return false;
        
        switch (propertyType)
        {
            case EPropertyType.BoolProperty:
            {
                type = "bool";
                value = propertyTag.GetGenericValue<bool>().ToString().ToLowerInvariant();
                break;
            }
            case EPropertyType.IntProperty:
            {
                type = "int";
                value = propertyTag.GetGenericValue<int>().ToString();
                break;
            }
            case EPropertyType.ObjectProperty:
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
            case EPropertyType.DoubleProperty:
            {
                type = "double";
                value = propertyTag.GetGenericValue<double>().ToString(CultureInfo.InvariantCulture);
                break;
            }
            case EPropertyType.ArrayProperty:
            {
                var scriptArray = propertyTag.GetGenericValue<UScriptArray>();

                if (scriptArray.Properties.Count == 0)
                {
                    value = "{}";
                    var innerType = scriptArray.InnerType switch
                    {
                        "BoolProperty" => "bool",
                        _ => throw new NotImplementedException($"Variable type of InnerType '{scriptArray.InnerType}' is currently not supported for UScriptArray")
                    };

                    type = $"TArray<{innerType}>";
                }
                else
                {
                    var customStringBuilder = new CustomStringBuilder();

                    customStringBuilder.OpenBlock();
                    foreach (var property in scriptArray.Properties)
                    {
                        if (!GetPropertyTagVariable(new FPropertyTag(new FName(scriptArray.InnerType), property), out type, out var innerValue))
                        {
                            Log.Error("Unable to get InnerType {type} value for UScriptArray", scriptArray.InnerType);
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
                    Log.Error("Unabled to get struct value or type for FScriptStruct type {structType}", structType.GetType().Name);
                    return false;
                }
                
                type = $"struct F{propertyTag.TagData?.StructType}";
                break;
            }
            case EPropertyType.EnumProperty:
            {
                value = propertyTag.GetGenericValue<FName>().ToString();
                type = $"enum {propertyTag.TagData?.EnumName}";
                break;
            }
        }
        
        return !string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(value);
    }
    
    private static bool GetPropertyTagVariable(FScriptStruct scriptStruct, out string value)
    {
        value = string.Empty;
        
        switch (scriptStruct.StructType)
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
                    foreach (var property in fallback.Properties)
                    {
                        stringBuilder.AppendLine(property.ToString());
                    }
                    stringBuilder.CloseBlock();
                
                    value = stringBuilder.ToString();
                }

                break;
            }
            default:
            {
                Log.Warning("Property Type '{type}' is currently not supported for FScriptStruct", scriptStruct.StructType.GetType().Name);
                break;
            }
        }
        
        return !string.IsNullOrWhiteSpace(value);
    }

    public static string GetLineExpression(KismetExpression kismetExpression)
    {
        string expression;

        switch (kismetExpression)
        {
            // whats the difference between localVariable and instanceVariable
            case EX_LocalVariable localVariable:
            {
                return localVariable.Variable.New.Path[0].ToString();
            }
            case EX_InstanceVariable instanceVariable:
            {
                return instanceVariable.Variable.New.Path[0].ToString();
            }
            case EX_LocalOutVariable localOutVariable:
            {
                return $"&{localOutVariable.Variable.New.Path[0].ToString()}";
            }
            case EX_LetValueOnPersistentFrame persistent:
            {
                var variableAssignment = GetLineExpression(persistent.AssignmentExpression);
                var variableToBeAssigned = persistent.DestinationProperty.New.Path[0].ToString();

                return $"{variableToBeAssigned} = {variableAssignment}";
            }
            case EX_Let let:
            {
                var assignement = GetLineExpression(let.Assignment);
                var variable = GetLineExpression(let.Variable);
                
                return $"{variable} = {assignement}";
            }
            case EX_LetBool letBool:
            {
                var variable = GetLineExpression(letBool.Variable);
                var assignment = GetLineExpression(letBool.Assignment);
                
                return $"{variable} = {assignment}";
            }
            case EX_Context_FailSilent failSilent:
            {
                var function = GetLineExpression(failSilent.ContextExpression);
                var obj = GetLineExpression(failSilent.ObjectExpression);

                var customStringBuilder = new CustomStringBuilder();
                
                customStringBuilder.AppendLine($"if ({obj})");
                customStringBuilder.IncreaseIndentation();
                customStringBuilder.Append($"{obj}->{function}");
                
                return customStringBuilder.ToString();
                
            }
            case EX_Context context:
            {
                var function = GetLineExpression(context.ContextExpression);
                var obj = GetLineExpression(context.ObjectExpression);

                return $"{obj}->{function}";
            }
            case EX_CallMath callMath:
            {
                var parametersList = new List<string>();
                foreach (var parameter in callMath.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }
                
                var parameters =  string.Join(", ", parametersList);
                var pkgIndex = callMath.StackNode.ToString();

                var classType = $"U{pkgIndex.SubstringAfter('.').SubstringBefore(':')}";
                var functionName = pkgIndex.SubstringAfter(':').SubstringBefore("'");

                return $"{classType}::{functionName}({parameters})";
            }
            case EX_LocalVirtualFunction localVirtualFunction:
            {
                var parametersList = new List<string>();
                foreach (var parameter in localVirtualFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                var functionName = localVirtualFunction.VirtualFunctionName.ToString();
                return $"this->{functionName}({parameters})";
            }
            case EX_VirtualFunction virtualFunction:
            {
                var parametersList = new List<string>();
                foreach (var parameter in virtualFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                return $"{virtualFunction.VirtualFunctionName.ToString()}({parameters})";
            }
            case EX_FinalFunction finalFunction:
            {
                var parametersList = new List<string>();
                foreach (var parameter in finalFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }
                
                var parameters =  string.Join(", ", parametersList);
                var pkgIndex = finalFunction.StackNode.ToString();

                var classType = $"U{pkgIndex.SubstringAfter('.').SubstringBefore(':')}";
                var functionName = pkgIndex.SubstringAfter(':').SubstringBefore("'");

                // shld it be "->" or "::"
                return $"{classType}::{functionName}({parameters})";
            }
            case EX_IntConst intConst:
            {
                return intConst.Value.ToString();
            }
            case EX_ByteConst byteConst:
            {
                return byteConst.Value.ToString();
            }
            case EX_ObjectConst objectConst:
            {
                var pkgIndex = objectConst.Value.ToString();
                var classType = $"U{pkgIndex.SubstringBefore("'")}";
                
                return $"FindObject<{classType}>(nullptr, TEXT(\"{pkgIndex}\"))";
            }
            case EX_NameConst nameConst:
            {
                return $"FName({nameConst.Value.ToString()})";
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
            case EX_ObjToInterfaceCast interfaceCast:
            {
                var target = GetLineExpression(interfaceCast.Target);
                var struc = $"I{interfaceCast.ClassPtr.ToString().SubstringBeforeLast("'").SubstringAfter('.')}";

                return $"Cast<{struc}*>({target})";
            }
            case EX_Cast cast:
            {
                var target = GetLineExpression(cast.Target);
                var conversionType = cast.ConversionType switch
                {
                    ECastToken.CST_InterfaceToBool => "bool",
                    ECastToken.CST_DoubleToFloat => "float",
                    _ => throw new NotImplementedException($"ConversionType {cast.ConversionType} is currently not implemented")
                };

                return $"Cast<{conversionType}>({target})";
            }
            case EX_PopExecutionFlowIfNot popExecutionFlowIfNot:
            {
                var booleanExpression = GetLineExpression(popExecutionFlowIfNot.BooleanExpression);
                var customStringBuilder = new CustomStringBuilder();
                
                customStringBuilder.AppendLine($"if (!{booleanExpression})");
                customStringBuilder.IncreaseIndentation();
                customStringBuilder.Append("FlowStack.Pop()");

                return customStringBuilder.ToString();
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
            case EX_NoObject:
            {
                return "nullptr";
            }
            case EX_Return:
            {
                return "return";
            }
            default:
                throw new NotImplementedException($"KismetExpression '{kismetExpression.GetType().Name}' is currently not supported");
        }
        
        return expression;
    }
}