using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
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

    private static string GetTextProperty(FKismetPropertyPointer property)
    {
        if (property.Old != null)
        {
            return property.Old?.Name ?? "UnknownOldVariable";
        }
        return string.Join('.', property.New.Path.Select(n => n.Text)).Replace(" ", "");
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
            case "ClassProperty":
            {
                var classType = (propertyType.Tag?.GenericValue as FPackageIndex)?.ResolvedObject?.Class?.Name.ToString();
                text = $"class U{classType ?? "Class"}*";
                break;
            }
            case "ObjectProperty":
            {
                var classType = (propertyType.Tag?.GenericValue as FPackageIndex)?.ResolvedObject?.Class?.Name.ToString();
                text = $"class U{classType ?? "Object"}*";
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
            case "NameProperty":
            {
                text = "FName";
                break;
            }
            case "FloatProperty":
            {
                text = "Float";
                break;
            }
            case "ByteProperty":
            {
                text = "Byte";
                break;
            }
            case "Int64Property":
            {
                text = "int";
                break;
            }
            case "UInt64Property":
            {
                text = "uint64";
                break;
            }
            case "UInt32Property":
            {
                text = "uint32";
                break;
            }
            case "TextProperty": // correct?
            case "StrProperty":
            {
                text = "string";
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
            case BoolProperty boolProperty:
            {
                text = boolProperty?.GenericValue.ToString() ?? "UnknownBoolProp"; // real?
                break;
            }
            case NameProperty nameProperty:
            {
                text = nameProperty?.GenericValue.ToString() ?? "UnknownNameProp"; // real?
                break;
            }
            case FloatProperty floatProperty:
            {
                text = floatProperty?.GenericValue.ToString() ?? "UnknownFloatProp"; // real?
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

    private static bool IsPointer(FProperty property) => property.PropertyFlags.HasFlag(EPropertyFlags.ReferenceParm) || property.PropertyFlags.HasFlag(EPropertyFlags.InstancedReference) ||property.PropertyFlags.HasFlag(EPropertyFlags.ContainsInstancedReference) || property.GetType() == typeof(FObjectProperty);

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
            case FClassProperty classProperty:
            {
                var classType = classProperty.PropertyClass.Name;

                value = classProperty.PropertyClass.ToString(); // todo:
                type += $"class U{classType}";

                break;
            }
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
            case FStrProperty:
            {
                type = "string";
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
                var pkgIndex = packageIndex.ToString();
                text = pkgIndex == "0" ? "nullptr" : $"\"{pkgIndex}\"";
                break;
            }
            case FScriptStruct scriptStruct:
            {
                text = GetPropertyText(scriptStruct);
                break;
            }
            case FMulticastScriptDelegate name:
            {
                text = name.ToString();
                break;
            }
            case FName name:
            {
                text = name.ToString();
                break;
            }
            case Byte byteText:
            {
                text = byteText.ToString();
                break;
            }
            case FText fText:
            {
                text = fText.ToString();
                break;
            }
            case NameProperty nameProp:
            {
                text = nameProp.ToString();
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
            case UInt64 uint64:
            {
                text = uint64.ToString();
                break;
            }
            case UInt32 uint32:
            {
                text = uint32.ToString();
                break;
            }
            case string str:
            {
                text = str;
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
                    var propertyText = GetPropertyText(property);
                    var propertyType = GetPropertyTagType(property);

                    stringBuilder.AppendLine(propertyText!);
                }

                stringBuilder.CloseBlock("]");

                text = stringBuilder.ToString();
                break;
            }
            case FVector vector:
            {
                text = $"FVector({vector.X}, {vector.Y}, {vector.Z})";
                break;
            }
            case FGuid guid:
            {
                text = $"FGuid({guid.A}, {guid.B}, {guid.C}, {guid.D})";
                break;
            }
            case TIntVector3<int> vector3:
            {
                text = $"FVector({vector3.X}, {vector3.Y}, {vector3.Z})";
                break;
            }
            case FVector4 vector4:
            {
                text = $"FVector4({vector4.X}, {vector4.Y}, {vector4.Z}, {vector4.W})";
                break;
            }
            case TIntVector3<float> floatVector3:
            {
                text = $"FVector({floatVector3.X}, {floatVector3.Y}, {floatVector3.Z})";
                break;
            }
            case TIntVector2<float> floatVector2:
            {
                text = $"FVector2D({floatVector2.X}, {floatVector2.Y})";
                break;
            }
            case FVector2D vector2d:
            {
                text = $"FVector2D({vector2d.X}, {vector2d.Y})";
                break;
            }
            case FQuat fQuat:
            {
                text = $"FQuat({fQuat.X}, {fQuat.Y}, {fQuat.Z}, {fQuat.W})";
                break;
            }
            case FRotator rotator:
            {
                text = $"FRotator({rotator.Pitch}, {rotator.Yaw}, {rotator.Roll})";
                break;
            }
            case FLinearColor linearColor:
            {
                text = $"FLinearColor({linearColor.R}, {linearColor.G}, {linearColor.B}, {linearColor.A})";
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
                // what is this vro, incorrect sometimes, fix.
                // CallFunc_GetViewportSize_ReturnValue = FindObject<UClass>("/Script/UMG.Default__WidgetLayoutLibrary")->UWidgetLayoutLibrary::GetViewportSize(this);
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

                if (variable is EX_InstanceVariable)
                {
                    throw new NotImplementedException();
                }

                expression = $"{GetTextProperty(variable)}";
                break;
            }
            case EX_NoObject:
            case EX_NoInterface:
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
                var convertedType = exCast.ConversionType switch
                {
                    ECastToken.CST_InterfaceToBool or ECastToken.CST_ObjectToBool or ECastToken.CST_InterfaceToBool2 or ECastToken.CST_ObjectToBool2 => "bool",
                    ECastToken.CST_DoubleToFloat => "float",
                    ECastToken.CST_FloatToDouble  => "double",
                    ECastToken.CST_ObjectToInterface  => "Interface", // make sure this makes sense
                    _ => throw new NotImplementedException() // impossible
                };

                expression = $"dynamic_cast<{convertedType}>({variable})";

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
                var assignmentVariable = GetLineExpression(letObj.Assignment);

                expression = $"{variable} = {assignmentVariable}";

                break;
            }
            case EX_Let let:
            {
                var variable = GetLineExpression(let.Variable);
                var assignment = GetLineExpression(let.Assignment);

                expression = $"{variable} = {assignment}";
                break;
            }

            case EX_RotationConst rot:
            {
                var value = rot.Value;
                expression = $"FRotator({value.Pitch}, {value.Yaw}, {value.Roll})";
                break;
            }
            case EX_VectorConst vec:
            {
                var value = vec.Value;
                expression = $"FVector({value.X}, {value.Y}, {value.Z})";
                break;
            }
            case EX_Vector3fConst vec3:
            {
                var value = vec3.Value;
                expression = $"FVector3f({value.X}, {value.Y}, {value.Z})";
                break;
            }
            case EX_TransformConst xf:
            {
                var value = xf.Value;
                expression =
                    $"FTransform(FQuat({value.Rotation.X}, {value.Rotation.Y}, {value.Rotation.Z}, {value.Rotation.W}), FVector({value.Translation.X}, {value.Translation.Y}, {value.Translation.Z}), FVector({value.Scale3D.X}, {value.Scale3D.Y}, {value.Scale3D.Z}))";
                break;
            }
            case EX_DoubleConst dbl:
            {
                var val = dbl.Value;
                expression = Math.Abs(val - Math.Floor(val)) < 1e-10 ? ((int) val).ToString() : val.ToString("R");
                break;
            }
            case EX_Int64Const i64:
            {
                expression = i64.Value.ToString();
                break;
            }
            case EX_UInt64Const ui64:
            {
                expression = ui64.Value.ToString();
                break;
            }
            case EX_SkipOffsetConst skip:
            {
                expression = skip.Value.ToString();
                break;
            }
            case EX_BitFieldConst bit:
            {
                expression = bit.ConstValue.ToString();
                break;
            }
            case EX_UnicodeStringConst uni:
            {
                expression = $"\"{uni.Value}\"";
                break;
            }
            case EX_StringConst stringConst:
            {
                expression = $"\"{stringConst.Value}\"";
                break;
            }
            case EX_InstanceDelegate del:
            {
                expression = $"\"{del.FunctionName}\"";
                break;
            }
            case EX_IntOne:
            {
                expression = "1";
                break;
            }
            case EX_IntZero:
            {
                expression = "0";
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
            case EX_Self:
            {
                expression = "this";
                break;
            }
            case EX_ObjectConst objectConst:
            {
                var objectString = objectConst?.Value?.ToString();

                if (!string.IsNullOrEmpty(objectString) && objectString.Contains('\''))
                {
                    var parts = objectString.Split('\'');
                    var typeName = parts[0];
                    var path = parts[1];

                    var classType = $"U{typeName}";
                    expression = $"FindObject<{classType}>(\"{path}\")";
                }
                else
                {
                    expression = $"FindObject<UObject>(\"{objectString}\")";
                }

                break;
            }
            case EX_SoftObjectConst softObjectConst:
            {
                var pathToObject = GetLineExpression(softObjectConst.Value);
                return $"FSoftObjectPath(\"{pathToObject}\")";
            }
            case EX_StructConst structConst:
            {
                var parameters = new List<string>();
                foreach (var property in structConst.Properties)
                {
                    parameters.Add(GetLineExpression(property));
                }

                var parametersString = string.Join(", ", parameters);
                var pkgIndex = structConst.Struct.ToString().SubstringAfter('.');
                var struc = $"F{pkgIndex}";

                return $"{struc}({parametersString})";
            }
            case EX_NameConst nameConst:
            {
                var nameValue = nameConst.Value.ToString();
                expression = $"\"{nameValue}\"";
                break;
            }
            case EX_IntConst intConst:
            {
                expression = intConst.Value.ToString();
                break;
            }
            case EX_FloatConst floatConst:
            {
                return floatConst.Value.ToString(CultureInfo.InvariantCulture);
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

                customStringBuilder.Append("return");
                customStringBuilder.DecreaseIndentation();

                expression = customStringBuilder.ToString();
                break;
            }
            case EX_ArrayGetByRef arrayGetByRef:
            {
                var arrayIndex = GetLineExpression(arrayGetByRef.ArrayIndex);
                var arrayVariable = GetLineExpression(arrayGetByRef.ArrayVariable);

                expression = $"{arrayVariable}[{arrayIndex}]";

                break;
            }
            case EX_ArrayConst arrayConst:
            {
                var variableName = string.Empty;

                var customStringBuilder = new CustomStringBuilder();
                if (arrayConst.Elements.Length > 0)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    customStringBuilder.Append($"TArray<void> {{ }}"); //  it's array in stuff such as Func({}})
                }

                return customStringBuilder.ToString();
            }
            case EX_SetArray setArray:
            {
                var variableName = string.Empty;
                if (setArray.AssigningProperty is not null)
                {
                    //variableName = GetLineExpression(setArray.AssigningProperty); // fix this vro
                }
                else if (setArray.ArrayInnerProp is not null)
                {
                    throw new NotImplementedException();
                }

                var customStringBuilder = new CustomStringBuilder();
                if (setArray.Elements.Length > 0)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    customStringBuilder.Append($"TArray<void> {variableName} = {{ }}");
                }

                return customStringBuilder.ToString();
            }
            case EX_DynamicCast dynamicCast:
            {
                var classPtr = $"U{dynamicCast.ClassPtr.Name}*";
                var variable = GetLineExpression(dynamicCast.Target);

                return $"dynamic_cast<{classPtr}>({variable})";
            }
            case EX_BindDelegate bindDelegate:
            {
                var variableName = GetLineExpression(bindDelegate.Delegate);
                var functionName = bindDelegate.FunctionName.ToString();

                return $"{variableName}->BindUFunction({functionName})";
            }
            case EX_AddMulticastDelegate multicastDelegate:
            {
                var variableName = GetLineExpression(multicastDelegate.Delegate);
                var delegateToAdd = GetLineExpression(multicastDelegate.DelegateToAdd);

                return $"{variableName}->AddDelegate({delegateToAdd})";
            }
            case EX_StructMemberContext structMemberContext:
            {
                var name = GetLineExpression(structMemberContext.StructExpression);
                var namee = GetTextProperty(structMemberContext.Property); // todo:

                return $"{name}.{namee}";
            }
            default:
                return $"KismetExpression '{kismetExpression.GetType().Name}' is currently not supported";
        }

        return expression;
    }
}
