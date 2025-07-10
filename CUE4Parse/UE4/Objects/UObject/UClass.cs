using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject.BlueprintDecompiler;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Objects.UObject
{
    [SkipObjectRegistration]
    public class UClass : UStruct
    {
        /** Used to check if the class was cooked or not */
        public bool bCooked;

        /** Class flags; See EClassFlags for more information */
        public EClassFlags ClassFlags; // EClassFlags

        /** The required type for the outer of instances of this class */
        public FPackageIndex ClassWithin;

        /** This is the blueprint that caused the generation of this class, or null if it is a native compiled-in class */
        public FPackageIndex ClassGeneratedBy;

        /** Which Name.ini file to load Config variables out of */
        public FName ClassConfigName;

        /** The class default object; used for delta serialization and object initialization */
        public FPackageIndex ClassDefaultObject;

        /** Map of all functions by name contained in this class */
        public Dictionary<FName, FPackageIndex /*UFunction*/> FuncMap;

        /**
         * The list of interfaces which this class implements, along with the pointer property that is located at the offset of the interface's vtable.
         * If the interface class isn't native, the property will be null.
         */
        public FImplementedInterface[] Interfaces;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Game == EGame.GAME_AWayOut) Ar.Position += 4;

            // serialize the function map
            FuncMap = new Dictionary<FName, FPackageIndex>();
            var funcMapNum = Ar.Read<int>();
            for (var i = 0; i < funcMapNum; i++)
            {
                FuncMap[Ar.ReadFName()] = new FPackageIndex(Ar);
            }

            // Class flags first.
            ClassFlags = Ar.Read<EClassFlags>();

            // Variables.
            if (Ar.Game is EGame.GAME_StarWarsJediFallenOrder or EGame.GAME_StarWarsJediSurvivor or EGame.GAME_AshesOfCreation) Ar.Position += 4;
            ClassWithin = new FPackageIndex(Ar);
            ClassConfigName = Ar.ReadFName();

            ClassGeneratedBy = new FPackageIndex(Ar);

            // Load serialized interface classes
            Interfaces = Ar.ReadArray(() => new FImplementedInterface(Ar));

            var bDeprecatedForceScriptOrder = Ar.ReadBoolean();
            var dummy = Ar.ReadFName();

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.ADD_COOKED_TO_UCLASS)
            {
                bCooked = Ar.ReadBoolean();
            }

            // Defaults.
            ClassDefaultObject = new FPackageIndex(Ar);
        }
        
        public Assets.Exports.UObject? ConstructObject(EObjectFlags flags)
        {
            var type = ObjectTypeRegistry.Get(Name);
            if (type is null && this is UBlueprintGeneratedClass && flags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
                type = typeof(Assets.Exports.UObject);
            if (type != null)
            {
                try
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance is Assets.Exports.UObject obj)
                    {
                        return obj;
                    }
                    else
                    {
                        Log.Warning("Class {Type} did have a valid constructor but does not inherit UObject", type);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Class {Type} could not be constructed", type);
                }
            }

            return null;
        }

        public string DecompileBlueprintToPseudo()
        {
            var derivedClass = BlueprintDecompilerUtils.GetClassWithPrefix(this);
            var accessSpecifier = Flags.HasFlag(EObjectFlags.RF_Public) ? "public" : "private";
            
            var superStruct = SuperStruct.Load<UStruct>();
            var baseClass = BlueprintDecompilerUtils.GetClassWithPrefix(superStruct);

            var stringBuilder = new CustomStringBuilder();
            
            stringBuilder.AppendLine($"class {derivedClass} : {accessSpecifier} {baseClass}");
            stringBuilder.OpenBlock();

            // Properties
            // TODO switch to this
            // var variables = new Dictionary<string, EAccessMode>();

            var existingVariables = new List<string>();
            
            var publicVariable = new List<string>();
            var protectedVariables = new List<string>();
            var privateVariables = new List<string>();
            
            foreach (var property in Properties)
            {
                if (existingVariables.Contains(property.Name.Text))
                    continue;
                
                var variableText = BlueprintDecompilerUtils.GetPropertyText(property);
                if (variableText is null)
                    continue;
                    
                var variableType = BlueprintDecompilerUtils.GetPropertyTagType(property);
                if (variableType is null)
                    continue;

                var variableExpression = $"{variableType} {property.Name.Text} = {variableText}";
                publicVariable.Add(variableExpression);
                
                existingVariables.Add(property.Name.Text);
            }

            if (ClassDefaultObject.TryLoad(out var classDefaultObject))
            {
                foreach (var property in classDefaultObject.Properties)
                {
                    if (existingVariables.Contains(property.Name.Text))
                        continue;
                    
                    var variableText = BlueprintDecompilerUtils.GetPropertyText(property);
                    if (variableText is null)
                        continue;
                    
                    var variableType = BlueprintDecompilerUtils.GetPropertyTagType(property);
                    if (variableType is null)
                        continue;

                    var variableExpression = $"{variableType} {property.Name.Text} = {variableText};";
                    protectedVariables.Add(variableExpression);
                    
                    existingVariables.Add(property.Name.Text);
                }
            }
            
            foreach (var childProperty in ChildProperties)
            {
                if (childProperty is not FProperty property)
                    continue;
                
                if (existingVariables.Contains(property.Name.Text))
                    continue;
                
                var (variableValue, variableType) = BlueprintDecompilerUtils.GetPropertyType(property);
                if (variableType is null)
                    continue;

                var value = variableValue is null ? string.Empty : $" = {variableValue}";
                var variableExpression = $"{variableType} {property.Name.Text}{value};";
                
                privateVariables.Add(variableExpression);
                
                existingVariables.Add(property.Name.Text);
            }

            if (publicVariable.Count > 0)
            {
                stringBuilder.DecreaseIndentation();
                stringBuilder.AppendLine("public:");
                stringBuilder.IncreaseIndentation();

                foreach (var property in publicVariable)
                {
                    stringBuilder.AppendLine(property);
                }
            }
            
            if (protectedVariables.Count > 0)
            {
                stringBuilder.DecreaseIndentation();
                stringBuilder.AppendLine("protected:");
                stringBuilder.IncreaseIndentation();

                foreach (var variable in protectedVariables)
                {
                    stringBuilder.AppendLine(variable);
                }
            }
            
            if (privateVariables.Count > 0)
            {
                stringBuilder.DecreaseIndentation();
                stringBuilder.AppendLine("private:");
                stringBuilder.IncreaseIndentation();

                foreach (var variable in privateVariables)
                {
                    stringBuilder.AppendLine(variable);
                }
            }
            
            if (FuncMap.Count > 0) stringBuilder.AppendLine();

            var totalFuncMapCount = FuncMap.Count;
            var index = 1;
            
            foreach (var (key, value) in FuncMap)
            {
                if (!value.TryLoad(out var export) || export is not UFunction function)
                    continue;

                var parametersList = new List<string>();
                foreach (var childProperty in function.ChildProperties)
                {
                    if (childProperty is not FProperty property)
                        continue;
                
                    var (_, variableType) = BlueprintDecompilerUtils.GetPropertyType(property);
                    if (variableType is null)
                        continue;
                
                    var parameterExpression = $"{variableType} {property.Name.Text}";
                    parametersList.Add(parameterExpression);
                }
                
                var parameters = string.Join(", ", parametersList);
                var returnType = function.ScriptBytecode[^2] switch
                {
                    EX_Return valid => valid.ReturnExpression switch
                    {
                        EX_Nothing => "void",
                        _ => throw new NotImplementedException($"ReturnExpression {valid.ReturnExpression.GetType().Name} is currently not implemented"),
                    }, 
                    
                    _ => throw new NotSupportedException()
                };
                
                var functionExpression = $"{returnType} {key.Text}({parameters})";
                
                var functionStringBuilder = new CustomStringBuilder();
                functionStringBuilder.AppendLine(functionExpression);
                functionStringBuilder.OpenBlock();
                
                foreach (var expression in function.ScriptBytecode)
                {
                    // TODO:
                }
                
                functionStringBuilder.CloseBlock();
                
                var functionBlock = functionStringBuilder.ToString();
                stringBuilder.AppendLine(functionBlock);
                if (index < totalFuncMapCount) stringBuilder.AppendLine();
                
                index++;
            }
            
            stringBuilder.CloseBlock("};");
            
            return stringBuilder.ToString();
        }
        
        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (FuncMap is { Count: > 0 })
            {
                writer.WritePropertyName("FuncMap");
                serializer.Serialize(writer, FuncMap);
            }

            if (ClassFlags != EClassFlags.CLASS_None)
            {
                writer.WritePropertyName("ClassFlags");
                writer.WriteValue(ClassFlags.ToStringBitfield());
            }

            if (ClassWithin is { IsNull: false })
            {
                writer.WritePropertyName("ClassWithin");
                serializer.Serialize(writer, ClassWithin);
            }

            if (!ClassConfigName.IsNone)
            {
                writer.WritePropertyName("ClassConfigName");
                serializer.Serialize(writer, ClassConfigName);
            }

            if (ClassGeneratedBy is { IsNull: false })
            {
                writer.WritePropertyName("ClassGeneratedBy");
                serializer.Serialize(writer, ClassGeneratedBy);
            }

            if (Interfaces is { Length: > 0 })
            {
                writer.WritePropertyName("Interfaces");
                serializer.Serialize(writer, Interfaces);
            }

            if (bCooked)
            {
                writer.WritePropertyName("bCooked");
                writer.WriteValue(bCooked);
            }

            if (ClassDefaultObject is { IsNull: false })
            {
                writer.WritePropertyName("ClassDefaultObject");
                serializer.Serialize(writer, ClassDefaultObject);
            }
        }

        public class FImplementedInterface
        {
            /** the interface class */
            public FPackageIndex Class;

            /** the pointer offset of the interface's vtable */
            public int PointerOffset;

            /** whether or not this interface has been implemented via K2 */
            public bool bImplementedByK2;

            public FImplementedInterface(FAssetArchive Ar)
            {
                Class = new FPackageIndex(Ar);
                PointerOffset = Ar.Read<int>();
                bImplementedByK2 = Ar.ReadBoolean();
            }
        }
    }
}

public enum EAccessMode : byte
{
    Public,
    Protected,
    Private
}