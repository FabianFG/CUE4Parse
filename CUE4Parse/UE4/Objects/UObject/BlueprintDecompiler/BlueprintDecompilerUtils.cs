using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CUE4Parse.GameTypes._2XKO.Kismet;
using CUE4Parse.MappingsProvider;
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
    public static TypeMappings? Mappings { get; set; }
    public static UFunction Function { get; set; }
    private static readonly Stack<int> _executionFlowStack = new();

    public static string GetClassWithPrefix(UStruct? prefixClassStruct)
    {
        var prefix = GetPrefix(prefixClassStruct);
        return $"{prefix}{prefixClassStruct?.Name}";
    }

    private static string GetPrefix(UStruct? struc)
    {
        var current = struc;

        while (current != null)
        {
            if (current.Name == "Actor")
                return "A";
            if (current.Name == "Interface")
                return "I";
            if (current.Name == "Object")
                return "U";

            var next = current.SuperStruct?.Load<UStruct>();

            if (next is null && Mappings is not null &&
                Mappings.Types.TryGetValue(current.Name, out var structMappings))
            {
                if (string.IsNullOrEmpty(structMappings.SuperType) ||
                    current.Name == structMappings.SuperType)
                    break;

                if (structMappings.SuperType == "Actor")
                    return "A";
                if (structMappings.SuperType == "Interface")
                    return "I";
                if (structMappings.SuperType == "Object")
                    return "U";

                next = new UScriptClass(structMappings.SuperType);
            }

            current = next;
        }
        return "U";
    }

    private static string GetPrefix(string? strucName)
    {
        if (string.IsNullOrEmpty(strucName) || Mappings is null)
            return "U";

        var current = strucName;

        if (current == "Actor") return "A";
        if (current == "Interface") return "I";
        if (current == "Object") return "U";

        while (Mappings.Types.TryGetValue(current, out var structMappings))
        {
            var superType = structMappings.SuperType;

            if (string.IsNullOrEmpty(superType) || superType == current)
                break;

            if (superType == "Actor") return "A";
            if (superType == "Interface") return "I";
            if (superType == "Object") return "U";

            current = superType;
        }

        return "U";
    }

    // Add_IntInt = UKismetMathLibrary::Add_IntInt(Temp_int_Loop_Counter_Variable, 1);
    // to
    // Add_IntInt = Temp_int_Loop_Counter_Variable + 1;
    private static string MathFunctionCleaner(
        string className,
        string functionName,
        List<string> parametersList,
        string parameters)
    {
        if (className.StartsWith("SolarisMathLibrary_") || className == "KismetMathLibrary")
        {
            if (functionName.StartsWith("EqualEqual_ByteByte")) return $"((!{parametersList[0]}) == (!{parametersList[1]}))";
            if (functionName.StartsWith("NotEqual_ByteByte")) return $"((!{parametersList[0]}) !== (!{parametersList[1]}))";
            if (functionName.StartsWith("EqualEqual_")) return $"{parametersList[0]} == {parametersList[1]}";
            if (functionName.StartsWith("NotEqual_")) return $"({parametersList[0]} !== {parametersList[1]})";
            if (functionName.StartsWith("NotEqualExactly_")) return $"({parametersList[0]} != {parametersList[1]})";
            if (functionName.StartsWith("LessEqual_")) return $"({parametersList[0]} <= {parametersList[1]})";
            if (functionName.StartsWith("Less_")) return $"({parametersList[0]} < {parametersList[1]})";
            if (functionName.StartsWith("GreaterEqual_")) return $"({parametersList[0]} >= {parametersList[1]})";
            if (functionName.StartsWith("Greater_")) return $"({parametersList[0]} > {parametersList[1]})";

            if (functionName.StartsWith("Add_")) return $"{parametersList[0]} + {parametersList[1]}";
            if (functionName.StartsWith("Xor_"))  return $"({parametersList[0]} ^ {parametersList[1]})";
            if (functionName.StartsWith("Multiply_")) return $"({parametersList[0]} * {parametersList[1]})";
            if (functionName.StartsWith("Percent_")) return $"({parametersList[0]} % {parametersList[1]})";
            if (functionName.StartsWith("Or_")) return $"({parametersList[0]} | {parametersList[1]})";
            if (functionName.StartsWith("Subtract_")) return $"{parametersList[0]} - {parametersList[1]}";
            if (functionName.StartsWith("Not_PreBool")) return $"!{parametersList[0]}";
            if (functionName.StartsWith("Not_")) return $"(~{parametersList[0]})";
            if (functionName.StartsWith("Select")) return $"({parametersList[2]} ? {parametersList[0]} : {parametersList[1]})";
            if (functionName.StartsWith("AddEquals")) return $"({parametersList[0]} += {parametersList[1]})";
            if (functionName.StartsWith("Subtract")) return $"({parametersList[0]} - {parametersList[1]})";
            if (functionName.StartsWith("Divide")) return $"({parametersList[0]} / {parametersList[1]})";
            if (functionName.StartsWith("Multiply")) return $"({parametersList[0]} * {parametersList[1]})";
            if (functionName.StartsWith("BooleanAND")) return $"{parametersList[0]} && {parametersList[1]}";
            if (functionName.StartsWith("BooleanNAND")) return $"!({parametersList[0]} && {parametersList[1]})";
            if (functionName.StartsWith("BooleanOR")) return $"({parametersList[0]} || {parametersList[1]})";
            if (functionName.StartsWith("BooleanXOR")) return $"{parametersList[0]} ^ {parametersList[1]}";
            if (functionName.StartsWith("BooleanNOR")) return $"!({parametersList[0]} || {parametersList[1]})";
            if (functionName.StartsWith("Floor")) return $"Floor({parametersList[0]})";
            if (functionName.StartsWith("Abs")) return $"{parametersList[0]} < 0.0 ? -{parametersList[0]} : {parametersList[0]}";
            if (functionName.StartsWith("CheckConstrainedFloat")) return $"{parametersList[2]} < {parametersList[0]} or {parametersList[2]} > {parametersList[1]}";
            if (functionName == "Max") return $"(({parametersList[0]} > {parametersList[1]}) ? {parametersList[0]} : {parametersList[1]})";
            if (functionName.StartsWith("Negate")) return $"-{parametersList[0]}";
            if (functionName.StartsWith("Ceil")) return $"Ceil({parametersList[0]})";

            if (functionName.StartsWith("UncheckedConvertI32I64")) return $"{parametersList[0]}";
            if (functionName.StartsWith("MakeTransform")) return $"FTransform({parametersList[0]}, {parametersList[1]}, {parametersList[2]})";
            if (functionName.StartsWith("Conv_VectorToTransform")) return $"FTransform({parametersList[0]})";
            if (functionName.StartsWith("MakeVector2D")) return $"FVector({parametersList[0]}, {parametersList[1]})";
            if (functionName.StartsWith("MakeVector")) return $"FVector({parametersList[0]}, {parametersList[1]}, {parametersList[2]})";
            if (functionName.EndsWith("ToVector")) return $"FVector((float){parametersList[0]})";
            if (functionName.StartsWith("MakeRotator")) return $"FRotator({parametersList[0]}, {parametersList[1]}, {parametersList[2]})";
            if (functionName.StartsWith("MakeTimespan")) return $"FTimespan({parametersList[0]}, {parametersList[1]}, {parametersList[2]}, {parametersList[4]} * 1000 * 1000)";
            if (functionName.StartsWith("MakeColor")) return $"FLinearColor({parametersList[0]}, {parametersList[1]}, {parametersList[2]}, {parametersList[3]})";
            if (functionName.StartsWith("ComposeRotators")) return $"FRotator(FQuat({parametersList[0]}) * FQuat({parametersList[1]}))";
            if (functionName.EndsWith("ToLinearColor")) return $"FLinearColor({parametersList[0]})";

            if (functionName.StartsWith("Conv_IntToBool")) return $"({parametersList[0]} != 0)";
            if (functionName.StartsWith("Conv_BoolToInt")) return $"({parametersList[0]} ? 1 : 0)";
            if (functionName.StartsWith("Conv_BoolToByte")) return $"({parametersList[0]} ? 1 : 0)";
            if (functionName.StartsWith("Conv_BoolToFloat")) return $"({parametersList[0]} ? 1.0f : 0.0f)";
            if (functionName.StartsWith("Conv_BoolToDouble")) return $"({parametersList[0]} ? 1.0 : 0.0)";

            if (functionName.EndsWith("ToDouble")) return $"((double){parametersList[0]})";
            if (functionName.EndsWith("ToFloat")) return $"((float){parametersList[0]})";
            if (functionName.EndsWith("ToInt64")) return $"((int64){parametersList[0]})";
            if (functionName.EndsWith("ToInt")) return $"((int32){parametersList[0]})";
            if (functionName.EndsWith("ToByte")) return $"((uint8){parametersList[0]})";

            if (functionName == "BreakRotator")
            {
                return $@"{parametersList[1]} = {parametersList[0]}.Roll;
{parametersList[2]} = {parametersList[0]}.Pitch;
{parametersList[3]} = {parametersList[0]}.Yaw";
            }
            if (functionName == "BreakVector")
            {
                return $@"{parametersList[1]} = {parametersList[0]}.X;
{parametersList[2]} = {parametersList[0]}.Y;
{parametersList[3]} = {parametersList[0]}.Z";
            }
            if (functionName == "BreakVector2D")
            {
                return $@"{parametersList[1]} = {parametersList[0]}.X;
{parametersList[2]} = {parametersList[0]}.Y";
            }
            if (functionName == "BreakTransform")
            {
                return $@"{parametersList[1]} = {parametersList[0]}.Location;
{parametersList[2]} = {parametersList[0]}.Rotation;
{parametersList[3]} = {parametersList[0]}.Scale";
            }
            if (functionName == "BreakColor")
            {
                return $@"{parametersList[1]} = {parametersList[0]}.R;
{parametersList[2]} = {parametersList[0]}.G;
{parametersList[3]} = {parametersList[0]}.B;
{parametersList[4]} = {parametersList[0]}.A";
            }
            if (functionName.StartsWith("Clamp"))
            {
                return $"(({parametersList[0]} < {parametersList[1]}) ? {parametersList[1]} : (({parametersList[0]} > {parametersList[2]}) ? {parametersList[2]} : {parametersList[0]}))";
            }
            if (functionName.StartsWith("Lerp"))
            {
                return $"{parametersList[0]} + {parametersList[2]} * ({parametersList[1]} - {parametersList[0]})";
            }
        }
        if (className == "KismetStringLibrary")
        {
            if (functionName.StartsWith("EqualEqual_")) return $"{parametersList[0]} == {parametersList[1]}";
            if (functionName.StartsWith("NotEqual_")) return $"({parametersList[0]} !== {parametersList[1]})";

            if (functionName.EndsWith("ToDouble")) return $"(double){parametersList[0]}";
            if (functionName.EndsWith("ToFloat")) return $"(float){parametersList[0]}";
            if (functionName.EndsWith("ToInt64")) return $"(int64){parametersList[0]}";
            if (functionName.EndsWith("ToInt")) return $"(int32){parametersList[0]}";
            if (functionName.EndsWith("ToByte")) return $"(uint8){parametersList[0]}";

            if (functionName.StartsWith("Conv_BoolToString")) return $"{parametersList[0]} ? \"true\" : \"false\"";
            if (functionName.EndsWith("ToString")) return $"FString({parametersList[0]})";
            if (functionName.EndsWith("ToName")) return $"FName({parametersList[0]})";
            if (functionName.StartsWith("Concat_StrStr")) return string.Join(" += ", parametersList);
            if (functionName.StartsWith("ParseIntoArray")) return $"{parametersList[0]}.Split({parametersList[1]}, /* removeEmpty = */ {parametersList[2]})";
            if (functionName.StartsWith("Contains")) return $"{parametersList[0]}.Contains({parametersList[1]}, /* removeEmpty = */ {parametersList[2]})";
            if (functionName.StartsWith("JoinStringArray")) return $"{parametersList[0]}.Join({parametersList[1]})";
            if (functionName.StartsWith("Replace")) return $"{parametersList[0]}.Replace({parametersList[1]}, {parametersList[2]}, /* SearchCase = */ {parametersList[3]})";
            if (functionName.StartsWith("StartsWith")) return $"{parametersList[0]}.startswith({parametersList[1]}, /* SearchCase = */ {parametersList[2]})";
            if (functionName.StartsWith("Contains")) return $"{parametersList[0]}.Contains({parametersList[1]}, /* bUseCase = */ {parametersList[2]}, /* bSearchFromEnd = */ {parametersList[3]})";
            if (functionName.StartsWith("IsNumeric")) return $"{parametersList[0]}.IsNumeric()";
            if (functionName.StartsWith("Len")) return $"{parametersList[0]}.Length";
        }
        if (className == "KismetSystemLibrary")
        {
            if (functionName.StartsWith("IsValid") || functionName.StartsWith("Conv_SoftClassReferenceToClass") || functionName.StartsWith("Conv_SoftObjectReferenceToObject") || (functionName.StartsWith("Make") && parametersList.Count == 1))
            {
                return $"{parametersList[0]}";
            }
            if (functionName.StartsWith("Conv_ObjectToSoftObjectReference") || functionName.StartsWith("Conv_SoftObjPathToSoftObjRef")) return $"TSoftObjectPtr<UObject>({parametersList[0]})";
            if (functionName.StartsWith("Delay") && parametersList.Count == 3) return $"Delay({parametersList[1]}f);\n{parametersList[2]}";
            if (functionName.StartsWith("Conv_SoftClassPathToSoftClassRef")) return $"TSoftClassPtr<UObject>({parametersList[0]})";
            if (functionName.StartsWith("Conv_ClassToSoftClassReference")) return $"TSoftClassPtr<UObject>(*{parametersList[0]})";
        }
        if (className == "KismetInputLibrary" || className == "BlueprintGameplayTagLibrary" || className == "FortKismetLibrary" || className == "KismetTextLibrary")
        {
            if (functionName.StartsWith("EqualEqual_")) return $"{parametersList[0]} == {parametersList[1]}";
            if (functionName.StartsWith("NotEqual_")) return $"({parametersList[0]} !== {parametersList[1]})";
            if (functionName.EndsWith("ToText")) return $"FText({parametersList[0]})";
            if (functionName.EndsWith("ToString"))  return $"FString({parametersList[0]})";
        }

        // Doesn't work on UE4 as GetPrefix requires mappings
        return $"{GetPrefix(className)}{className}::{functionName}({parameters})";
    }

    private static string FinalFunctionCleaner(
        string className,
        string functionName,
        List<string> parametersList,
        string parameters)
    {
        if (className == "KismetArrayLibrary")
        {
            if (functionName.StartsWith("Array_Length")) return $"{parametersList[0]}.Length";
            if (functionName.StartsWith("Array_IsNotEmpty")) return $"{parametersList[0]}.Length > 0";
            if (functionName.StartsWith("Array_LastIndex")) return $"{parametersList[0]}.Length - 1";
            if (functionName.StartsWith("Array_Clear")) return $"{parametersList[0]}.Clear()";
            if (functionName.StartsWith("Array_Identical")) return $"{parametersList[0]} == {parametersList[1]}";
            if (functionName.StartsWith("Array_Remove")) return $"{parametersList[0]}.Remove({parametersList[1]})";
            if (functionName.StartsWith("Array_Add")) return $"{parametersList[0]}.Add({parametersList[1]})";
            if (functionName.StartsWith("Array_Get")) return $"{parametersList[2]} = {parametersList[0]}[{parametersList[1]}]";
            if (functionName.StartsWith("Array_Contains")) return $"{parametersList[0]}[{parametersList[1]}]";
            if (functionName.StartsWith("Array_IsValidIndex")) return $"{parametersList[0]}[{parametersList[1]}]";
            if (functionName.StartsWith("Array_Insert")) return $"{parametersList[0]}[{parametersList[2]}] = {parametersList[1]}";
        }

        if (className == "BlueprintMapLibrary")
        {
            if (functionName.StartsWith("Map_Length")) return $"{parametersList[0]}.Length";
            if (functionName.StartsWith("Map_Remove")) return $"{parametersList[0]}.Remove({parametersList[1]})";
            if (functionName.StartsWith("Map_Contains")) return $"{parametersList[0]}[{parametersList[1]}]";
            if (functionName.StartsWith("Map_Get")) return $"{parametersList[2]} = {parametersList[0]}[{parametersList[1]}]";
        }

        if (className == "BlueprintSetLibrary")
        {
            if (functionName.StartsWith("Set_AddItems")) return $"{parametersList[0]}.Add({parametersList[1]})";
            if (functionName.StartsWith("Set_Clear")) return $"{parametersList[0]}.Clear()";
            if (functionName.StartsWith("Set_Difference")) return $"{parametersList[2]} = {parametersList[0]} == {parametersList[1]}";
            if (functionName.StartsWith("Set_IsEmpty")) return $"{parametersList[0]}.Length == 0";
        }

        // Doesn't work on UE4 as GetPrefix requires mappings
        return $"{GetPrefix(className)}{className}::{functionName}({parameters})";
    }


    private static string GetTagTypes(FPropertyTagData? tagType) => tagType?.Type switch
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
            "VerseStringProperty" => "string",
            "DoubleProperty" => "double",
            "NameProperty" => "FName",
            "TextProperty" => "FText",
            "FloatProperty" => "float",
            "SoftObjectProperty" or "AssetObjectProperty" => "FSoftObjectPath",
            "ObjectProperty" or "ClassProperty" => "UObject*",
            "StructProperty" => $"F{tagType.StructType}",
            "InterfaceProperty" => $"I{tagType.StructType}",
            _ => throw new NotSupportedException($"PropertyType {tagType?.Type} is currently not supported")
        };

    private static bool IsPointer(FProperty property) => property.PropertyFlags.HasFlag(EPropertyFlags.ReferenceParm) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags.InstancedReference) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags.ContainsInstancedReference) ||
                                                         property.GetType() == typeof(FObjectProperty);

    private static bool IsPointer(UProperty property) => property.PropertyFlags.HasFlag(EPropertyFlags.ReferenceParm) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags.InstancedReference) ||
                                                         property.PropertyFlags.HasFlag(EPropertyFlags.ContainsInstancedReference) ||
                                                         property.GetType() == typeof(UObjectProperty);

    public static (string?, string?) GetPropertyType(FProperty property)
    {
        string? value = null;
        string? type = null;

        var propertyFlags = property.PropertyFlags;
        if (propertyFlags.HasFlag(EPropertyFlags.ConstParm))
        {
            type += "const ";
        }

        switch (property)
        {
            case FObjectProperty objectProperty:
            {
                // Looks bad and provides useless information.
                // value = objectProperty.PropertyClass.ToString();
                type += $"class {GetClassWithPrefix(objectProperty.PropertyClass.Load<UStruct>())}";
                break;
            }
            case FArrayProperty arrayProperty:
            {
                var (_, innerType) = GetPropertyType(arrayProperty.Inner!);

                // Looks bad and provides useless information.
                //var customStringBuilder = new CustomStringBuilder();
                //customStringBuilder.OpenBlock("[");
                //customStringBuilder.AppendLine(innerValue!);
                //customStringBuilder.CloseBlock("]");

                //value = customStringBuilder.ToString();
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
            case FVerseStringProperty:
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
            case FMapProperty mapProperty:
            {
                var (_, keyinnerType) = GetPropertyType(mapProperty.KeyProp!);
                var (_, valueinnerType) = GetPropertyType(mapProperty.ValueProp!);
                type = $"TMap<{keyinnerType}, {valueinnerType}>";
                break;
            }
            case FEnumProperty enumProperty:
            {
                type = $"{enumProperty.Enum.Name}";
                break;
            }
            case FVerseDynamicProperty:
            {
                type = "DynamicVerse";
                break;
            }
            case FVerseFunctionProperty:
            {
                type = "VerseFunc";
                break;
            }
            case FOptionalProperty optionalProperty:
            {
                var (_, keyinnerType) = GetPropertyType(optionalProperty.ValueProperty!);
                type = $"TOptional<{keyinnerType}>";
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

    // Legacy UProperty Support
     public static (string?, string?) GetPropertyType(UProperty property)
    {
        string? value = null;
        string? type = null;

        var propertyFlags = property.PropertyFlags;
        if (propertyFlags.HasFlag(EPropertyFlags.ConstParm))
        {
            type += "const ";
        }

        switch (property)
        {
            case UObjectProperty objectProperty:
            {
                // Looks bad and provides useless information.
                // value = objectProperty.PropertyClass.ToString();
                type += $"class {GetClassWithPrefix(objectProperty.PropertyClass.Load<UStruct>())}";
                break;
            }
            case UArrayProperty arrayProperty:
            {
                if (arrayProperty.Inner.Load() is UProperty innerProp)
                {
                    var (_, innerType) = GetPropertyType(innerProp);

                    // Looks bad and provides useless information.
                    //var customStringBuilder = new CustomStringBuilder();
                    //customStringBuilder.OpenBlock("[");
                    //customStringBuilder.AppendLine(innerValue!);
                    //customStringBuilder.CloseBlock("]");

                    //value = customStringBuilder.ToString();
                    type += $"TArray<{innerType}>";
                }
                break;
            }
            case UStructProperty structProperty:
            {
                var structType = structProperty.Struct.Name;

                type += $"struct F{structType}";
                value = structProperty.Struct.ToString();
                break;
            }
            case UNumericProperty:
            {
                if (property is UByteProperty byteProperty && byteProperty.Enum.TryLoad(out var enumObj))
                {
                    type = enumObj.Name;
                }
                else
                {
                    type = property.GetType().Name.SubstringAfter("U").SubstringBefore("Property").ToLowerInvariant();
                }
                break;
            }
            case UInterfaceProperty interfaceProperty:
            {
                type = $"F{interfaceProperty.InterfaceClass.Name}";
                break;
            }
            case UBoolProperty boolProperty:
            {
                type = boolProperty.bIsNativeBool ? "bool" : "uint8";
                break;
            }
            case UStrProperty:
            {
                type = "FString";
                break;
            }
            case UTextProperty:
            {
                type = "FText";
                break;
            }
            case UNameProperty:
            {
                type = "FName";
                break;
            }
            case UMapProperty mapProperty:
            {
                if (mapProperty.KeyProp.Load() is UProperty innerProp && mapProperty.KeyProp.Load() is UProperty valueProp)
                {
                    var (_, keyinnerType) = GetPropertyType(innerProp);
                    var (_, valueinnerType) = GetPropertyType(valueProp);
                    type = $"TMap<{keyinnerType}, {valueinnerType}>";
                }
                break;
            }
            case UEnumProperty enumProperty:
            {
                type = $"{enumProperty.Enum.Name}";
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
    private static string GetGenericValueStr<T>(this FPropertyTag propertyTag) => propertyTag.Tag?.GenericValue?.ToString()!;

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
                    value = propertyTag.GetGenericValueStr<byte>();
                    type = "byte";
                }

                break;
            }
            case EPropertyType.BoolProperty:
            {
                type = "bool";
                value = propertyTag.GetGenericValueStr<bool>().ToLowerInvariant();
                break;
            }
            case EPropertyType.IntProperty:
            {
                type = "int32";
                value = propertyTag.GetGenericValueStr<int>();
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
                var pkgIndex = propertyTag.GetGenericValueStr<FPackageIndex>();
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
                value = $"FName(\"{propertyTag.GetGenericValueStr<FName>()}\")";
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
                if (scriptArray == null || scriptArray.Properties == null || scriptArray.InnerType == null || scriptArray.InnerTagData == null)
                {
                    value = "{}";
                    type = "TArray<unknown>";
                    break;
                }

                if (scriptArray.Properties.Count == 0)
                {
                    value = "{}";
                    var innerType = GetTagTypes(scriptArray.InnerTagData);

                    type = $"TArray<{innerType}>";
                }
                else
                {
                    var customStringBuilder = new CustomStringBuilder();
                    customStringBuilder.OpenBlock();
                    for (int i = 0; i < scriptArray.Properties.Count; i++)
                    {
                        var property = scriptArray.Properties[i];
                        if (!GetPropertyTagVariable(
                                new FPropertyTag(new FName(scriptArray.InnerType), property, scriptArray.InnerTagData),
                                out type, out var innerValue))
                        {
                            Log.Warning("Failed to get ArrayElement of type {type}", scriptArray.InnerType);
                            continue;
                        }

                        if (scriptArray.InnerType == "EnumProperty")
                        {
                            innerValue = innerValue.SubstringAfter("::");
                        }

                        if (i < scriptArray.Properties.Count - 1)
                            customStringBuilder.AppendLine(innerValue + ",");
                        else
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
                value = $"\"{propertyTag.GetGenericValueStr<string>()}\"";
                break;
            }
            case EPropertyType.VerseStringProperty:
            {
                type = "FString";
                value = $"\"{propertyTag.GetGenericValueStr<string>()}\"";
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
                var softObjectPath = propertyTag.GetGenericValueStr<FSoftObjectPath>();

                type = "FSoftObjectPath";
                value = $"FSoftObjectPath(\"{softObjectPath}\")";
                break;
            }
            case EPropertyType.UInt64Property:
            {
                type = "uint64";
                value = propertyTag.GetGenericValueStr<ulong>();
                break;
            }
            case EPropertyType.UInt32Property:
            {
                type = "uint32";
                value = propertyTag.GetGenericValueStr<uint>();
                break;
            }
            case EPropertyType.UInt16Property:
            {
                type = "uint16";
                value = propertyTag.GetGenericValueStr<ushort>();
                break;
            }
            case EPropertyType.Int64Property:
            {
                type = "int64";
                value = propertyTag.GetGenericValueStr<long>();
                break;
            }
            case EPropertyType.Int16Property:
            {
                type = "int16";
                value = propertyTag.GetGenericValueStr<short>();
                break;
            }
            case EPropertyType.Int8Property:
            {
                type = "int8";
                value = propertyTag.GetGenericValueStr<sbyte>();
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
                    var keyType = GetTagTypes(propertyTag.TagData?.InnerTypeData);
                    var valueType = GetTagTypes(propertyTag.TagData?.ValueTypeData);

                    type = $"TMap<{keyType}, {valueType}>";
                    value = "{}";
                }

                break;
            }
            case EPropertyType.EnumProperty:
            {
                value = propertyTag.GetGenericValueStr<FName>(); // .SubstringAfter("::")
                type = $"enum";//{propertyTag.TagData?.EnumName}
                break;
            }
            case EPropertyType.FieldPathProperty:
            {
                value = propertyTag.GetGenericValueStr<FFieldPath>();
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
                type = "FMulticastScriptDelegate";
                return true;
            }
            case EPropertyType.VerseFunctionProperty:
            {
                type = "VerseFunction";
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
            case FIntPoint intPoint:
            {
                var x = intPoint.X;
                var y = intPoint.Y;
                value = $"FIntPoint({x}, {y})";
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
                value = $"FDateTime(\"{dateTime}\")";
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
                value = uStruct.ToString() ?? string.Empty;
                Log.Warning("Property Type '{type}' is currently not supported for FScriptStruct", uStruct.GetType().Name);
                break;
            }
        }

        return !string.IsNullOrWhiteSpace(value);
    }

    public static string GetLineExpression(KismetExpression expression)
    {
        switch (expression)
        {
            case EX_VariableBase variableBase:
            {
                return variableBase.Variable.ToString();
            }
            case EX_LetValueOnPersistentFrame persistent:
            {
                var variableAssignment = GetLineExpression(persistent.AssignmentExpression);
                var variableToBeAssigned = persistent.DestinationProperty.ToString();
                return $"{(variableToBeAssigned.Contains("K2Node_") ? "UberGraphFrame->" + variableToBeAssigned : variableToBeAssigned)} = {variableAssignment}";
            }
            case EX_LetBool letBool:
            {
                var assignment = GetLineExpression(letBool.Assignment);
                var variable = GetLineExpression(letBool.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_Let let:
            {
                var assignment = GetLineExpression(let.Assignment);
                var variable = GetLineExpression(let.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_LetBase letBase:
            {
                var assignment = GetLineExpression(letBase.Assignment);
                var variable = GetLineExpression(letBase.Variable);

                return $"{variable} = {assignment}";
            }
            case EX_Context context:
            {
                var function = context?.ContextExpression is not null ? GetLineExpression(context?.ContextExpression).SubstringAfter("::") : "failedplaceholder";
                var obj = context?.ObjectExpression is not null ? GetLineExpression(context?.ObjectExpression) : "failedplaceholder";

                var customStringBuilder = new CustomStringBuilder();
                if (expression is EX_Context_FailSilent)
                {
                    customStringBuilder.AppendLine($"if ({obj})");
                    customStringBuilder.IncreaseIndentation();
                }

                if (obj == "FindObject<UObject>(nullptr, this)" || obj.Contains("KismetArrayLibrary") || (!function.EndsWith("Map_Find") && obj.Contains("BlueprintMapLibrary")) || obj.Contains("BlueprintSetLibrary"))
                {
                    customStringBuilder.Append(function);
                }
                else
                {
                    customStringBuilder.Append($"{obj}->{function}");
                }

                return customStringBuilder.ToString();
            }
            case EX_FinalFunction final:
            {
                var parametersList = new List<string>(final.Parameters.Length);
                foreach (var parameter in final.Parameters)
                {
                    var prm = GetLineExpression(parameter);
                    if (!string.IsNullOrWhiteSpace(prm))
                    {
                        parametersList.Add(prm);
                    }
                }

                var parameters = string.Join(", ", parametersList);
                var stackNode = final.StackNode.ToString();
                var functionName = stackNode.SubstringAfter(':').Trim('\'');
                var className = stackNode.SubstringAfter('.').SubstringBefore(':');

                if (expression is EX_CallMath) return MathFunctionCleaner(className, functionName, parametersList, parameters);
                if (expression is EX_LocalFinalFunction) return $"{(stackNode.Contains("/Script/") ? $"{GetPrefix(className)}{className}::{functionName}" : functionName)}({parameters})";

                return FinalFunctionCleaner(className, functionName, parametersList, parameters);
            }
            case EX_VirtualFunction virtualFunc:
            {
                var parametersList = new List<string>(virtualFunc.Parameters.Length);
                foreach (var parameter in virtualFunc.Parameters)
                {
                    parametersList.Add(GetLineExpression(parameter));
                }

                var parameters = string.Join(", ", parametersList);
                var functionName = virtualFunc.VirtualFunctionName.Text;

                return $"{functionName}({parameters})";
            }
            case EX_TextConst textConst:
            {
                return textConst.Value.SourceString is null ? "nullptr" : GetLineExpression(textConst.Value.SourceString);
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
                var values = new List<string>(constArray.Elements.Length);
                foreach (var element in constArray.Elements)
                {
                    values.Add(GetLineExpression(element));
                }

               // var arrayProp = constArray.InnerProperty.New.ResolvedOwner.Load<UArrayProperty>();
               // var objProp = arrayProp.Inner.Load<UObjectProperty>();
              //  return objProp.PropertyClass?.Name ?? "Unknown";

                return $"TArray<{constArray.InnerProperty}>({string.Join(", ", values)})";
            }
            case EX_SetArray setArray:
            {
                var variable = GetLineExpression(setArray.AssigningProperty);

                var values = new List<string>(setArray.Elements.Length);
                foreach (var element in setArray.Elements)
                {
                    values.Add(GetLineExpression(element));
                }

                return $"{variable} = {(values.Count > 0 ? "[ " + string.Join(", ", values) + " ]" : "[]")}";
            }
            case EX_IntConst intConst:
            {
                return intConst.Value.ToString();
            }
            case KismetExpression<byte> byteConst:
            {
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

                    var classPkgType = $"U{typeName}";
                    return $"FindObject<{classPkgType}>(nullptr, \"{path}\")";
                }

                // package not found, sometimes it also is a "this"
                return $"FindObject<UObject>(nullptr, \"{pkgIndex}\")";
            }
            case EX_NameConst nameConst:
            {
                return $"\"{nameConst.Value.Text}\"";
            }
            case EX_Vector3fConst vectorF:
            {
                var value = vectorF.Value;
                return $"FVector3f({value.X}, {value.Y}, {value.Z})";
            }
            case EX_VectorConst vectorD:
            {
                var value = vectorD.Value;
                return $"FVector({value.X}, {value.Y}, {value.Z})";
            }
            case EX_TransformConst xf:
            {
                var value = xf.Value;
                return $"FTransform(FQuat({value.Rotation.X}, {value.Rotation.Y}, {value.Rotation.Z}, {value.Rotation.W}), FVector({value.Translation.X}, {value.Translation.Y}, {value.Translation.Z}), FVector({value.Scale3D.X}, {value.Scale3D.Y}, {value.Scale3D.Z}))";
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
            case KismetExpression<string> stringConst:
            {
                return $"\"{stringConst.Value.Replace("\r\n", "\\n").Replace("\n", "\\n")}\"";
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
                    ECastToken.CST_ObjectToBool or ECastToken.CST_ObjectToBool2 or ECastToken.CST_InterfaceToBool or ECastToken.CST_InterfaceToBool2 => "bool",
                    ECastToken.CST_DoubleToFloat => "float",
                    ECastToken.CST_FloatToDouble => "double",
                    ECastToken.CST_ObjectToInterface => "Interface",
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

                if (_executionFlowStack.Count == 0)
                {
                    customStringBuilder.Append("return");
                }
                else
                {
                    var target = _executionFlowStack.Pop();
                    customStringBuilder.Append($"goto Label_{target}");
                }

                return customStringBuilder.ToString();
            }
            case EX_PushExecutionFlow pushExecutionFlow:
            {
                var targetIndex = (int)pushExecutionFlow.PushingAddress;

                _executionFlowStack.Push(targetIndex);
                return "";
            }
            case EX_PopExecutionFlow:
            {
                if (_executionFlowStack.Count == 0)
                    return "return";

                var target = _executionFlowStack.Pop();
                return $"goto Label_{target}";
            }
            case EX_JumpIfNot jumpIfNot:
            {
                var booleanExpression = GetLineExpression(jumpIfNot.BooleanExpression);
                var customStringBuilder = new CustomStringBuilder();
                customStringBuilder.AppendLine($"if (!{booleanExpression})");
                customStringBuilder.IncreaseIndentation();
                var targetIndex = (int)jumpIfNot.CodeOffset;
                targetIndex = Array.FindIndex(Function.ScriptBytecode, stmt => stmt.StatementIndex == targetIndex);
                if (targetIndex >= 0 && targetIndex < Function.ScriptBytecode.Length && (Function.ScriptBytecode[targetIndex] is EX_Return || Function.ScriptBytecode[targetIndex++] is EX_Return))
                {
                    customStringBuilder.Append($"return");
                }
                else
                {
                    customStringBuilder.Append($"goto Label_{jumpIfNot.CodeOffset}");
                }

                return customStringBuilder.ToString();
            }
            case EX_Jump jump:
            {
                var targetIndex = (int)jump.CodeOffset;
                targetIndex = Array.FindIndex(Function.ScriptBytecode, stmt => stmt.StatementIndex == targetIndex);
                if (targetIndex >= 0 && targetIndex < Function.ScriptBytecode.Length && (Function.ScriptBytecode[targetIndex] is EX_Return || Function.ScriptBytecode[targetIndex++] is EX_Return))
                {
                    return "return";
                }

                return $"goto Label_{jump.CodeOffset}";
            }
            case EX_SkipOffsetConst skipOffsetConst:
            {
                return $"goto Label_{skipOffsetConst.Value}";
            }
            case EX_ComputedJump computedJump:
            {
                if (computedJump.CodeOffsetExpression is EX_VariableBase)
                {
                    return $"goto {GetLineExpression(computedJump.CodeOffsetExpression)}";
                }

                return GetLineExpression(computedJump.CodeOffsetExpression);
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
                if (returnExpr.ReturnExpression.Token == EExprToken.EX_Nothing)
                {
                    return "return";
                }

                var value = GetLineExpression(returnExpr.ReturnExpression);
                return $"return {value}";
            }
            case EX_SoftObjectConst objectConst:
            {
                var value = GetLineExpression(objectConst.Value);
                return $"FSoftObjectPath({value})";
            }
            case EX_FieldPathConst fieldPathConst:
            {
                var value = GetLineExpression(fieldPathConst.Value);
                return value;
            }
            case EX_CastBase cast:
            {
                var variable = GetLineExpression(cast.Target);
                var classType = cast.ClassPtr.Name;

                string castFunc;
                switch (expression.Token)
                {
                    case EExprToken.EX_MetaCast:
                        castFunc = $"CastClass<{GetClassWithPrefix(cast.ClassPtr.Load<UStruct>())}>";
                        break;
                    case EExprToken.EX_DynamicCast:
                    case EExprToken.EX_CrossInterfaceCast:
                    case EExprToken.EX_InterfaceToObjCast:
                        castFunc = $"Cast<{GetClassWithPrefix(cast.ClassPtr.Load<UStruct>())}>";
                        break;
                    case EExprToken.EX_ObjToInterfaceCast:
                        castFunc = $"Cast<{GetClassWithPrefix(cast.ClassPtr.Load<UStruct>())}*>";
                        break;
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
                var functionName = $"FName(\"{bindDelegate.FunctionName.Text}\")";

                return $"{delegateVar}->BindUFunction({objectTerm}, {functionName})";
            }
            case EX_StructConst structConst:
            {
                var properties = new List<string>(structConst.Properties.Length);
                foreach (var property in structConst.Properties)
                {
                    properties.Add(GetLineExpression(property));
                }

                if (structConst.Struct.Name == "LatentActionInfo") return properties[0]; // used for cleaning code output.

                return $"F{structConst.Struct.Name}({string.Join(", ", properties)})";
            }
            case EX_FloatConst floatConst:
            {
                return floatConst.Value.ToString(CultureInfo.CurrentCulture);
            }
            case EX_DoubleConst doubleConst:
            {
                return doubleConst.Value.ToString(CultureInfo.CurrentCulture);
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

                var stringBuilder = new CustomStringBuilder();

                // todo: add inner type for TMap Set/Const
                //FortniteGame/Content/UI/InGame/HUD/WBP_QuickEditGrid.uasset
                //<{keyinnerType}, {valueinnerType}>
                stringBuilder.Append($"{target} = TMap {{ ");

                var elements = setMap.Elements;

                for (int i = 0; i < elements.Length; i += 2)
                {
                    var keyText = GetLineExpression(elements[i]);
                    var valueText = GetLineExpression(elements[i + 1]);

                    stringBuilder.Append($"{keyText}: {valueText}");

                    if (i + 2 < elements.Length)
                        stringBuilder.Append(", ");
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

                var elements = mapConst.Elements;

                for (int i = 0; i < elements.Length; i += 2)
                {
                    var keyText = GetLineExpression(elements[i]);
                    var valueText = GetLineExpression(elements[i + 1]);

                    stringBuilder.Append($"{keyText}: {valueText}");

                    if (i + 2 < elements.Length)
                        stringBuilder.Append(", ");
                }

                stringBuilder.Append(" }");
                return stringBuilder.ToString();
            }
            case EX_SwitchValue switchValue:
            {
                if (switchValue.Cases.Length == 2)
                {
                    var indexTerm = GetLineExpression(switchValue.IndexTerm);

                    var case0 = GetLineExpression(switchValue.Cases[0].CaseTerm);
                    var case1 = GetLineExpression(switchValue.Cases[1].CaseTerm);

                    return $"{indexTerm} ? {case1} : {case0}";
                }

                var stringBuilder = new CustomStringBuilder();
                stringBuilder.AppendLine($"switch ({GetLineExpression(switchValue.IndexTerm)})");
                stringBuilder.OpenBlock();

                foreach (var caseItem in switchValue.Cases)
                {
                    stringBuilder.AppendLine($"case {GetLineExpression(caseItem.CaseIndexValueTerm)}:");
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
            case EX_StructMemberContext structMemberContext:
            {
                var property = structMemberContext.Property.ToString();
                var structExpression = GetLineExpression(structMemberContext.StructExpression);

                return $"{structExpression}.{property}";
            }
            case EX_CallMulticastDelegate callMulticastDelegate:
            {
                var parameters = new List<string>(callMulticastDelegate.Parameters.Length);
                foreach (var parameter in callMulticastDelegate.Parameters)
                {
                    parameters.Add(GetLineExpression(parameter));
                }

                var parametersString = string.Join(", ", parameters);

                var callDelegate = GetLineExpression(callMulticastDelegate.Delegate);
                return $"{callDelegate}->Broadcast({parametersString})";
            }
            case EX_RemoveMulticastDelegate removeMulticastDelegate:
            {
                var delegateExpr = removeMulticastDelegate.Delegate;
                var delegateTarget = GetLineExpression(delegateExpr);
                var delegateToRemove = GetLineExpression(removeMulticastDelegate.DelegateToAdd);

                var separator = delegateExpr.Token == EExprToken.EX_Context ? "->" : ".";

                return $"{delegateTarget}{separator}RemoveDelegate({delegateToRemove})";
            }
            case EX_ClearMulticastDelegate clearMulticastDelegate:
            {
                var delegateTarget = GetLineExpression(clearMulticastDelegate.DelegateToClear);
                return $"{delegateTarget}.Clear()";
            }
            case EX_PropertyConst propertyConst:
            {
                return propertyConst.Property.ToString();
            }
            case EX_FixedPointConst fp:
            {
                return fp.Value.ToString();
            }
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
            case EX_EndOfScript:
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
                throw new NotImplementedException($"KismetExpression '{expression.GetType().Name}' is currently not supported");
        }
    }
}
