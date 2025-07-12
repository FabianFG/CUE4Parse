using System;
using System.Collections.Generic;
using System.Globalization;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Kismet;
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
            case "ObjectProperty":
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
                text = boolean.ToString().ToLowerInvariant();
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

    public static string GetLineExpression(KismetExpression kismetExpression)
    {
        string expression;

        switch (kismetExpression)
        {
            case EX_LetValueOnPersistentFrame persistentFrame:
            {
                var variableAssignment = GetLineExpression(persistentFrame.AssignmentExpression);
                var destinationVariable = persistentFrame.DestinationProperty.New?.Path[0].Text;

                expression = $"{destinationVariable} = {variableAssignment}";

                break;
            }
            case EX_FinalFunction finalFunction:
            {
                var parametersList = new List<string>();
                foreach (var parameter in finalFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }
                
                var parameters = string.Join(", ", parametersList);
                
                var funcClass = finalFunction.StackNode.ToString().SubstringAfter('.').SubstringBefore(':');
                var funcName = finalFunction.StackNode.Name;

                expression = $"U{funcClass}::{funcName}({parameters})";
                break;
            }
            case EX_VirtualFunction virtualFunction:
            {
                var parametersList = new List<string>();
                foreach (var parameter in virtualFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }
                
                var funcName = virtualFunction.VirtualFunctionName.ToString();
                var parameters = string.Join(", ", parametersList);

                expression = $"{funcName}({parameters})";
                break;
            }
            case EX_VariableBase variableBase:
            {
                var variable = variableBase.Variable;
                if (variable.Old != null)
                {
                    throw new NotImplementedException();
                }

                expression = $"{variable.New?.Path[0].Text ?? "UnknownVariable"}";
                break;
            }
            case EX_NoObject:
            {
                expression = "nullptr";
                break;
            }
            case EX_Context_FailSilent failSilent:
            {
                var objectExpression = GetLineExpression(failSilent.ObjectExpression);
                var contextExpression = GetLineExpression(failSilent.ContextExpression);

                var customStringBuilder = new CustomStringBuilder();
                
                customStringBuilder.AppendLine($"if (!{objectExpression}->{contextExpression})");
                customStringBuilder.IncreaseIndentation();
                
                customStringBuilder.Append("return");
                customStringBuilder.DecreaseIndentation();
                
                expression = customStringBuilder.ToString();
                break;
            }
            case EX_Context context:
            {
                var contextExpression = GetLineExpression(context.ContextExpression);
                var objectExpression = GetLineExpression(context.ObjectExpression);
                
                expression = $"{objectExpression}->{contextExpression}";
                break;
            }
            case EX_Cast exCast:
            {
                var variable = GetLineExpression(exCast.Target);
                var conversionCast = exCast.ConversionType switch
                {
                    ECastToken.CST_InterfaceToBool => "reinterpret_cast<bool>",
                    ECastToken.CST_DoubleToFloat => "static_cast<float>",
                    _ => throw new NotImplementedException()
                };
                
                expression = $"{conversionCast}({variable})";
                
                break;
            }
            case EX_ObjToInterfaceCast objToInterfaceCast:
            {
                var classPtr = $"U{objToInterfaceCast.ClassPtr.Name}*";
                var variable = GetLineExpression(objToInterfaceCast.Target);
                
                expression = $"dynamic_cast<{classPtr}>({variable})";
                break;
            }
            case EX_InterfaceContext interfaceContext:
            {
                var interfaceValue = GetLineExpression(interfaceContext.InterfaceValue);
                expression = interfaceValue;
                break;
            }
            case EX_LetBase letObj:
            {
                var variable = GetLineExpression(letObj.Variable);
                var function = GetLineExpression(letObj.Assignment);
                
                expression = $"{variable} = {function}";
                
                break;
            }
            case EX_Let let:
            {
                var variable = GetLineExpression(let.Variable);
                var assignment = GetLineExpression(let.Assignment);

                expression = $"{variable} = {assignment}";
                break;
            }
            case EX_Self:
            {
                expression = "this";
                break;
            }
            case EX_ObjectConst objectConst:
            {
                var objString = objectConst.Value.ToString();
                var className = $"U{objString.SubstringBefore("'")}";
                expression = $"FindObject<{className}>({objString})";
                
                break;
            }
            case EX_NameConst nameConst:
            {
                var nameValue = nameConst.Value.ToString();
                expression = nameValue == "None" ? "NAME_None" : $"FName(\"{nameValue}\")";
                break;
            }
            case EX_IntConst intConst:
            {
                expression = intConst.Value.ToString();
                break;
            }
            case EX_ByteConst byteConst:
            {
                expression = byteConst.Value.ToString();
                break;
            }
            case EX_Return:
            {
                expression = "return";
                break;
            }
            case EX_PopExecutionFlowIfNot popExecutionFlowIfNot:
            {
                var booleanVariable = GetLineExpression(popExecutionFlowIfNot.BooleanExpression);
                
                var customStringBuilder = new CustomStringBuilder();
                
                customStringBuilder.AppendLine($"if (!{booleanVariable})");
                customStringBuilder.IncreaseIndentation();
                
                customStringBuilder.Append("FlowStack.Pop()");
                customStringBuilder.DecreaseIndentation();
                
                expression = customStringBuilder.ToString();
                break;
            }
            case EX_PopExecutionFlow:
            {
                expression = "FlowStack.Pop()";
                break;
            }
            case EX_ArrayGetByRef arrayGetByRef:
            {
                var arrayIndex = GetLineExpression(arrayGetByRef.ArrayIndex);
                var arrayVariable = GetLineExpression(arrayGetByRef.ArrayVariable);
                
                expression = $"{arrayVariable}[{arrayIndex}]";
                
                break;
            }
            case EX_True:
            {
                expression = "true";
                break;
            }
            case EX_False:
            {
                expression = "false";
                break;
            }
            default:
                throw new NotImplementedException($"KismetExpression '{kismetExpression.GetType().Name}' is currently not supported");
        }
        
        return expression;
    }
}