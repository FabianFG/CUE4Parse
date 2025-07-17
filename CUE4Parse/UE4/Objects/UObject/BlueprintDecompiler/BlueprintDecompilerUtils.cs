using System;
using System.Collections.Generic;
using System.Globalization;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Ai;
using CUE4Parse.UE4.Objects.Engine.GameFramework;
using CUE4Parse.UE4.Objects.GameplayTags;
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

    private static T GetGenericValue<T>(this FPropertyTag propertyTag) => (T) propertyTag.Tag?.GenericValue!;

    public static bool GetPropertyTagVariable(FPropertyTag propertyTag, out string type, out string value)
    {
        type = string.Empty;
        value = string.Empty;

        if (!Enum.TryParse<EPropertyType>(propertyTag.PropertyType.ToString(), out var propertyType))
            return false;

        switch (propertyType)
        {
            case EPropertyType.ByteProperty:
                if (propertyTag.Tag.GenericValue.ToString().Contains("::")) // idk how to check if FName
                {
                    var enumValue = propertyTag.GetGenericValue<FName>().ToString();
                    value = $"\"{enumValue}\"";
                    type =
                        $"enum {enumValue.Split("::")[0]}"; // si? enum EFortEncounterDirection PreviousStormShieldCoreEncounterDirection = "EFortEncounterDirection::Max_None"; change if you want
                }
                else
                {
                    value = propertyTag.GetGenericValue<byte>().ToString();
                }

                break;

            case EPropertyType.BoolProperty:
                type = "bool";
                value = propertyTag.GetGenericValue<bool>().ToString().ToLowerInvariant();
                break;

            case EPropertyType.IntProperty:
                type = "int32";
                value = propertyTag.GetGenericValue<int>().ToString();
                break;

            case EPropertyType.FloatProperty:
                type = "float";
                value = propertyTag.GetGenericValue<float>().ToString(CultureInfo.InvariantCulture);
                break;

            case EPropertyType.WeakObjectProperty:
            case EPropertyType.LazyObjectProperty:
            case EPropertyType.AssetObjectProperty:
            case EPropertyType.SoftObjectProperty:
                break; // todo
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

            case EPropertyType.NameProperty:
                type = "FName";
                value = $"\"{propertyTag.GetGenericValue<FName>()}\"";
                break;

            case EPropertyType.DelegateProperty:
                type = "FDelegateProperty";
                value = propertyTag.GetGenericValue<FScriptDelegate>().ToString();
                break;

            case EPropertyType.DoubleProperty:
                type = "double";
                value = propertyTag.GetGenericValue<double>().ToString(CultureInfo.InvariantCulture);
                break;

            case EPropertyType.ArrayProperty:
            {
                var scriptArray = propertyTag.GetGenericValue<UScriptArray>();
                if (scriptArray.Properties.Count == 0)
                {
                    value = "{}";
                    var innerType = scriptArray.InnerType switch
                    {
                        "BoolProperty" => "bool",
                        _ => throw new NotImplementedException(
                            $"Variable type of InnerType '{scriptArray.InnerType}' is currently not supported for UScriptArray")
                    };
                    type = $"TArray<{innerType}>";
                }
                else
                {
                    var StringBuilder = new CustomStringBuilder();
                    StringBuilder.OpenBlock();
                    foreach (var prop in scriptArray.Properties)
                    {
                        if (!GetPropertyTagVariable(new FPropertyTag(new FName(scriptArray.InnerType), prop), out type,
                                out var innerValue))
                        {
                            Log.Error("Failed to parse array element of type {type}",
                                scriptArray
                                    .InnerType); // todo: Failed to parse array element of type SoftObjectProperty
                            continue;
                        }

                        StringBuilder.AppendLine(innerValue);
                    }

                    StringBuilder.CloseBlock();
                    type = $"TArray<{type}>";
                    value = StringBuilder.ToString();
                }

                break;
            }
            case EPropertyType.StructProperty:
            {
                var structType = propertyTag.GetGenericValue<FScriptStruct>();
                if (!GetPropertyTagVariable(structType, out value))
                {
                    Log.Error("Unabled to get struct value or type for FScriptStruct type {structType}",
                        structType.GetType().Name);
                    return false;
                }

                type = $"struct F{propertyTag.TagData?.StructType}";
                break;
            }

            case EPropertyType.StrProperty:
                type = "FString";
                value = $"\"{propertyTag.GetGenericValue<string>()}\"";
                break;

            case EPropertyType.TextProperty:
                type = "FText";
                value = $"\"{propertyTag.GetGenericValue<FText>()}\"";
                break;

            case EPropertyType.InterfaceProperty:
                type = "TScriptInterface<IInterface>";
                value = propertyTag.GetGenericValue<FScriptInterface>().ToString();
                break;

            case EPropertyType.MulticastDelegateProperty:
                type = "FMulticastDelegateProperty";
                value = propertyTag.GetGenericValue<FMulticastScriptDelegate>().ToString();
                break;

            case EPropertyType.UInt64Property:
                type = "uint64";
                value = propertyTag.GetGenericValue<ulong>().ToString();
                break;

            case EPropertyType.UInt32Property:
                type = "uint32";
                value = propertyTag.GetGenericValue<uint>().ToString();
                break;

            case EPropertyType.UInt16Property:
                type = "uint16";
                value = propertyTag.GetGenericValue<ushort>().ToString();
                break;

            case EPropertyType.Int64Property:
                type = "int64";
                value = propertyTag.GetGenericValue<long>().ToString();
                break;

            case EPropertyType.Int16Property:
                type = "int16";
                value = propertyTag.GetGenericValue<short>().ToString();
                break;

            case EPropertyType.Int8Property:
                type = "int8";
                value = propertyTag.GetGenericValue<sbyte>().ToString();
                break;

            case EPropertyType.MapProperty:
            {
                var scriptMap = propertyTag.GetGenericValue<UScriptMap>();
/*
                if (scriptMap.Properties.Count > 0)
                {
                    var StringBuilder = new CustomStringBuilder();
                    StringBuilder.OpenBlock();
                    foreach (var pair in scriptMap.Properties)
                    {
                        var key = pair.Key;
                        var valuee = pair.Value;

                        var keyType = key.GetType().Name;
                        var valueType = valuee?.GetType().Name ?? "null";
                        if (!GetPropertyTagVariable(new FPropertyTag(new FName(keyType), pair.Key),
                                out var keyTypeResolved, out var keyValue) ||
                            !GetPropertyTagVariable(new FPropertyTag(new FName(valueType), pair.Value),
                                out var valueTypeResolved, out var valueValue))
                        {
                            Log.Error("Failed to parse map entry with key type {keyType} and value type {valueType}",
                                keyType, valueType);
                            continue;
                        }

                        StringBuilder.AppendLine($"[{keyValue}] = {valueValue}");
                        type = $"TMap<{keyTypeResolved}, {valueTypeResolved}>";
                    }

                    StringBuilder.CloseBlock();
                    value = StringBuilder.ToString();
                }*/ // idk you do this fr

                break;
            }

            case EPropertyType.SetProperty:
                type = $"struct TSet<F{propertyTag.TagData?.StructType}>";
                value = "a"; // todo:
                break;

            case EPropertyType.EnumProperty:
                value = propertyTag.GetGenericValue<FName>().ToString();
                type = $"enum {propertyTag.TagData?.EnumName}";
                break;

            case EPropertyType.FieldPathProperty:
                type = "FFieldPath";
                value = propertyTag.GetGenericValue<FFieldPath>().ToString();
                break;

            case EPropertyType.OptionalProperty:
                type = $"struct TOptional<F{propertyTag.TagData?.StructType}>";
                value = "a"; // todo:
                break;

            case EPropertyType.Utf8StrProperty:
            case EPropertyType.AnsiStrProperty:
                type = "FString";
                value = $"\"{propertyTag.GetGenericValue<string>()}\"";
                break;
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
                var a = guid.A;
                var b = guid.B;
                var c = guid.C;
                var d = guid.D;
                value = $"FGuid({a}, {b}, {c}, {d})";
                break;
            }
            case TIntVector3<int> vector3:
            {
                var x = vector3.X;
                var y = vector3.Y;
                var z = vector3.Z;
                value = $"FVector({x}, {y}, {z})";
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
            case TIntVector3<float> floatVector3:
            {
                var x = floatVector3.X;
                var y = floatVector3.Y;
                var z = floatVector3.Z;
                value = $"FVector({x}, {y}, {z})";
                break;
            }
            case TIntVector2<float> floatVector2:
            {
                var x = floatVector2.X;
                var y = floatVector2.Y;
                value = $"FVector2D({x}, {y})";
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
            case FBox box:
            {
                var maxX = box.Max.X;
                var maxY = box.Max.Y;
                var maxZ = box.Max.Z;
                var minX = box.Min.X;
                var minY = box.Min.Y;
                var minZ = box.Min.Z;
                value = $"FBox(FVector({maxX}, {maxY}, {maxZ}), FVector({minX}, {minY}, {minZ}))";
                break;
            }
            case FBox2D box2D:
            {
                var maxX = box2D.Max.X;
                var maxY = box2D.Max.Y;
                var minX = box2D.Min.X;
                var minY = box2D.Min.Y;
                value = $"FBox2D(FVector2D({maxX}, {maxY}), FVector2D({minX}, {minY}))";
                break;
            }
            case FDateTime dateTime:
            {
                value = $"FDateTime({dateTime})";
                break;
            }
            case FGameplayTagContainer gameplaytag:
            {
                value = $"FGameplayTagContainer()"; // todo:
                break;
            }
            default:
            {
                Log.Warning("Property Type '{type}' is currently not supported for FScriptStruct",
                    scriptStruct.StructType.GetType().Name);
                break;
            }
        }

        return !string.IsNullOrWhiteSpace(value);
    }

    public static string GetLineExpression(KismetExpression kismetExpression)
    {
        // TODO: Everything that include Const will have the const keyword at the start **maybe**
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
                var function = GetLineExpression(failSilent.ContextExpression).SubstringAfter("::");
                var obj = GetLineExpression(failSilent.ObjectExpression);

                var customStringBuilder = new CustomStringBuilder();

                customStringBuilder.AppendLine($"if ({obj})");
                customStringBuilder.IncreaseIndentation();
                customStringBuilder.Append($"{obj}->{function}");

                return customStringBuilder.ToString();
            }
            case EX_Context context:
            {
                var function = GetLineExpression(context.ContextExpression).SubstringAfter("::");
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

                var parameters = string.Join(", ", parametersList);
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
            case EX_TextConst textConst:
            {
                if (textConst.Value is FScriptText scriptText)
                {
                    if (scriptText.SourceString == null)
                    {
                        return "nullptr";
                    }

                    return GetLineExpression(scriptText.SourceString);
                }

                return textConst.Value?.ToString() ?? "nullptr";
            }
            case EX_FinalFunction finalFunction:
            {
                var parametersList = new List<string>();
                foreach (var parameter in finalFunction.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                var pkgIndex = finalFunction.StackNode.ToString();

                var classType = $"U{pkgIndex.SubstringAfter('.').SubstringBefore(':')}";
                var functionName = pkgIndex.SubstringAfter(':').SubstringBefore("'");

                return $"{classType}::{functionName}({parameters})";
            }
            case EX_ArrayConst arrayConst:
            {
                var variableName = string.Empty;

                var customStringBuilder = new CustomStringBuilder();
                if (arrayConst.Elements.Length > 0)
                {
                    //throw new NotImplementedException();
                }
                else
                {
                    customStringBuilder.Append($"TArray<void> {{ }}"); //  it's array in stuff such as Func({}})
                }

                return customStringBuilder.ToString();
            }
            case EX_SetArray setArray:
            {
                var variable = GetLineExpression(setArray.AssigningProperty);
                var value = string.Empty;

                if (setArray.Elements.Length > 0)
                {
                    var values = new List<string>();
                    foreach (var element in setArray.Elements)
                    {
                        values.Add(GetLineExpression(element));
                    }

                    value = string.Join(", ", values);
                }
                else
                {
                    value = "[]";
                }

                return $"{variable} = {value}";
            }
            case EX_IntConst intConst:
            {
                return intConst.Value.ToString();
            }
            case EX_ByteConst:
            case EX_IntConstByte:
            {
                var byteConst = (EX_ByteConst) kismetExpression; // idk i'm losing it man
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
                customStringBuilder.Append($"goto Label_{booleanExpression}"); // CodeOffset can't be gotten

                return ""; //customStringBuilder.ToString();
            }
            case EX_Jump jump:
            {
                return $"goto Label_{jump.CodeOffset}";
            }
            case EX_ComputedJump jump:
            {
                return ""; // $"goto {GetLineExpression(jump)}"; this also not possible rn
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
            case EX_Return:
            {
                return "return";
            }
            case EX_SoftObjectConst objectConst:
            {
                var stringValue = GetLineExpression(objectConst.Value);
                return $"FSoftObjectPath(\"{stringValue})\"";
            }
            case EX_MetaCast:
            case EX_DynamicCast:
            case EX_CrossInterfaceCast: // check these later? REAL idk
            case EX_InterfaceToObjCast:
            {
                var Cast = (EX_CastBase)kismetExpression; // i wanna throw up REAL
                var variable = GetLineExpression(Cast.Target);
                var classType = Cast.ClassPtr.Name;

                return $"Cast<{classType}>({variable})";
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

                var properties = new List<string>();
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
            case EX_SwitchValue switchValue:
            {
                if (switchValue.Cases.Length == 2)
                {
                    var indexTerm = GetLineExpression(switchValue.IndexTerm);

                    var case1 = GetLineExpression(switchValue.Cases[0].CaseIndexValueTerm);
                    var case2 = GetLineExpression(switchValue.Cases[1].CaseIndexValueTerm);

                    return $"{indexTerm} ? {case1} : {case2}";
                }

                var StringBuilder = new CustomStringBuilder();

                StringBuilder.AppendLine($"switch ({GetLineExpression(switchValue.IndexTerm)})");
                StringBuilder.OpenBlock();

                foreach (var caseItem in switchValue.Cases)
                {
                    string caseLabel;
                    if (caseItem.CaseIndexValueTerm.Token == EExprToken.EX_IntConst)
                    {
                        caseLabel = ((EX_IntConst) caseItem.CaseIndexValueTerm).Value.ToString();
                    }
                    else
                    {
                        caseLabel = GetLineExpression(caseItem.CaseIndexValueTerm);
                    }

                    StringBuilder.AppendLine($"case {caseLabel}:");
                    StringBuilder.OpenBlock();

                    StringBuilder.AppendLine($"return {GetLineExpression(caseItem.CaseTerm)};");
                    StringBuilder.AppendLine("break;");

                    StringBuilder.CloseBlock("}\n");
                }

                StringBuilder.AppendLine("default:");
                StringBuilder.OpenBlock();

                StringBuilder.AppendLine($"return {GetLineExpression(switchValue.DefaultTerm)};");
                StringBuilder.AppendLine("break;");

                StringBuilder.CloseBlock();

                StringBuilder.CloseBlock();

                return StringBuilder.ToString();
            }
            case EX_DoubleConst doubleConst:
            {
                return doubleConst.Value.ToString(CultureInfo.CurrentCulture);
            }
            case EX_StructMemberContext structMemberContext:
            {
                if (structMemberContext.Property.New?.Path.Count > 1)
                    throw new NotImplementedException();

                var property = structMemberContext.Property.New?.Path[0].ToString();
                var structExpression = GetLineExpression(structMemberContext.StructExpression);

                return $"{structExpression}.{property}";
            }
            case EX_CallMulticastDelegate callMulticastDelegate:
            {
                var delegatee = GetLineExpression(callMulticastDelegate.Delegate);

                var parameters = new List<string>();
                foreach (var parameter in callMulticastDelegate.Parameters)
                {
                    parameters.Add(GetLineExpression(parameter));
                }

                var parametersString = string.Join(", ", parameters);
                var functionName = callMulticastDelegate.StackNode.Name;

                return
                    $"{delegatee}->Broadcast({parametersString})"; // TODO: show the functionName somehow, maybe with comments added "// {funcName}"
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
            case EX_SkipOffsetConst skipOffsetConst: // TODO: What the fuck is this
            {
                return skipOffsetConst.Value.ToString(CultureInfo.CurrentCulture);
            }

            // todo:
            //EX_PropertyConst
            //EX_FieldPathConst
            case EX_VectorConst vectorConst:
            {
                var x = vectorConst.Value.X;
                var y = vectorConst.Value.Y;
                var z = vectorConst.Value.Z;

                return $"FVector({x}, {y}, {z})";
            }
            default:
                throw new NotImplementedException(
                    $"KismetExpression '{kismetExpression.GetType().Name}' is currently not supported");
        }
    }
}
