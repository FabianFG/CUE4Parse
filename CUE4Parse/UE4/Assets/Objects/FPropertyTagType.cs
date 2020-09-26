using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Assets.Objects
{
    public enum ReadType
    {
        NORMAL,
        MAP,
        ARRAY
    }

    public abstract class FPropertyTagType<T> : FPropertyTagType
    {
        public T Value { get; protected set; }
    }

    public abstract class FPropertyTagType
    {

        public object? GetValue(Type type)
        {
            if (this is FPropertyTagType<object> prop && type == prop.Value.GetType())
                return prop.Value;
            else if (this is FPropertyTagType<UScriptStruct> structProp && structProp.Value.StructType.GetType() == type)
                return structProp.Value.StructType;
            else if (this is FPropertyTagType<UScriptArray> arrayProp && type.IsArray)
            {
                var array = arrayProp.Value.Properties;
                var contentType = type.GetElementType()!;
                var result = Array.CreateInstance(contentType, array.Count);
                for (var i = 0; i < array.Count; i++)
                {
                    result.SetValue(array[i].GetValue(contentType), i);
                }
                return result;
            }
            else if (this is FPropertyTagType<FPackageIndex> objProp && typeof(UExport).IsAssignableFrom(type))
            {
                throw new NotImplementedException("Need to implement FileProvider first");
            }
            else if (this is FPropertyTagType<FSoftObjectPath> softObjProp && typeof(UExport).IsAssignableFrom(type))
            {
                throw new NotImplementedException("Need to implement FileProvider first");
            }
            else if (this is EnumProperty enumProp && type.IsEnum)
            {
                var storedEnum = enumProp.Value.Text;
                if (type.Name != storedEnum.SubstringBefore("::"))
                    return null;
                
                var search = storedEnum.SubstringAfter("::");
                var values = type.GetEnumNames()!;
                var idx = Array.FindIndex(values, it => it == search);
                return idx == -1 ? null : type.GetEnumValues().GetValue(idx);
            }
            //TODO Maybe Maps?
            return null;
        }
        
        internal static FPropertyTagType? ReadPropertyTagType(FAssetArchive Ar, string propertyType, FPropertyTagData? tagData, ReadType type)
        {
            switch(propertyType)
            {
                case "ByteProperty":
                    switch (type)
                    {
                        case ReadType.NORMAL:
                            var nameIndex = Ar.Read<int>();
                            if (nameIndex >= 0 && nameIndex < Ar.Owner.NameMap.Length)
                                return new NameProperty(new FName(Ar.Owner.NameMap, nameIndex, Ar.Read<int>()));
                            else
                                return new ByteProperty((byte) nameIndex);
                        case ReadType.MAP: return new ByteProperty((byte) Ar.Read<int>());
                        case ReadType.ARRAY: return new ByteProperty(Ar.Read<byte>());
                    }
                    break;
                case "BoolProperty":
                    switch (type)
                    {
                        case ReadType.NORMAL: return new BoolProperty((tagData as FPropertyTagData.BoolProperty)?.BoolVal == true);
                        case ReadType.MAP:
                        case ReadType.ARRAY:
                            return new BoolProperty(Ar.ReadFlag());
                    }
                    break;
                case "IntProperty": return new IntProperty(Ar.Read<int>());
                case "FloatProperty": return new FloatProperty(Ar.Read<float>());
                case "ObjectProperty": return new ObjectProperty(new FPackageIndex(Ar));
                case "NameProperty": return new NameProperty(Ar.ReadFName());
                case "DelegateProperty": return new DelegateProperty(Ar.Read<int>(), Ar.ReadFName());
                case "DoubleProperty": return new DoubleProperty(Ar.Read<double>());
                case "StrProperty": return new StrProperty(Ar.ReadFString());
                case "TextProperty": return new TextProperty(new FText(Ar));
                case "InterfaceProperty": return new InterfaceProperty(Ar.Read<UInterfaceProperty>());
                case "AssetObjectProperty":
                case "SoftObjectProperty":
                    var property = new SoftObjectProperty(new FSoftObjectPath(Ar));
                    if (type == ReadType.MAP)
                        Ar.Position += 4;
                    return property;
                case "UInt64Property": return new UInt64Property(Ar.Read<ulong>());
                case "UInt32Property": return new UInt32Property(Ar.Read<uint>());
                case "UInt16Property": return new UInt16Property(Ar.Read<ushort>());
                case "Int64Property": return new Int64Property(Ar.Read<long>());
                case "Int16Property": return new Int16Property(Ar.Read<short>());
                case "Int8Property": return new Int8Property(Ar.Read<sbyte>());
                case "EnumProperty":
                    if (type == ReadType.NORMAL && (tagData as FPropertyTagData.EnumProperty)?.EnumName.IsNone == true)
                        return new EnumProperty(new FName());
                    else
                        return new EnumProperty(Ar.ReadFName());
                case "ArrayProperty":
                    return tagData switch
                    {
                        FPropertyTagData.ArrayProperty arrayProperty => new ArrayProperty(new UScriptArray(Ar, arrayProperty.InnerType.Text)),
                        _ => null
                    };
                case "SetProperty":
                    return tagData switch
                    {
                        FPropertyTagData.SetProperty setProperty => new SetProperty(new UScriptArray(Ar, setProperty.InnerType.Text)),
                        _ => null,
                    };
                case "StructProperty": return new StructProperty(new UScriptStruct(Ar, (tagData as FPropertyTagData.StructProperty)?.StructName.Text));
                case "MapProperty":
                    return tagData switch
                    {
                        FPropertyTagData.MapProperty mapProperty => new MapProperty(new UScriptMap(Ar, mapProperty)),
                        _ => null,
                    };
                default:
#if DEBUG
                    Console.WriteLine($"Couldn't read property type {propertyType} at {Ar.Position}");         
#endif
                    return null;
            }
            return null;
        }

        public class BoolProperty : FPropertyTagType<bool>
        {
            public BoolProperty(bool value)
            {
                Value = value;
            }
        }
        
        public class ObjectProperty : FPropertyTagType<FPackageIndex>
        {
            public ObjectProperty(FPackageIndex value)
            {
                Value = value;
            }
        }
        
        public class InterfaceProperty : FPropertyTagType<UInterfaceProperty>
        {
            public InterfaceProperty(UInterfaceProperty value)
            {
                Value = value;
            }
        }
        
        public class FloatProperty : FPropertyTagType<float>
        {
            public FloatProperty(float value)
            {
                Value = value;
            }
        }

        public class StrProperty : FPropertyTagType<string>
        {
            public StrProperty(string value)
            {
                Value = value;
            }
        }
        
        public class NameProperty : FPropertyTagType<FName>
        {
            public NameProperty(FName value)
            {
                Value = value;
            }
        }
        
        public class IntProperty : FPropertyTagType<int>
        {
            public IntProperty(int value)
            {
                Value = value;
            }
        }
        
        public class UInt16Property : FPropertyTagType<ushort>
        {
            public UInt16Property(ushort value)
            {
                Value = value;
            }
        }
        
        public class UInt32Property : FPropertyTagType<uint>
        {
            public UInt32Property(uint value)
            {
                Value = value;
            }
        }
        
        public class UInt64Property : FPropertyTagType<ulong>
        {
            public UInt64Property(ulong value)
            {
                Value = value;
            }
        }
        
        public class Int64Property : FPropertyTagType<long>
        {
            public Int64Property(long value)
            {
                Value = value;
            }
        }
        
        public class Int16Property : FPropertyTagType<short>
        {
            public Int16Property(short value)
            {
                Value = value;
            }
        }
        
        public class Int8Property : FPropertyTagType<sbyte>
        {
            public Int8Property(sbyte value)
            {
                Value = value;
            }
        }
        
        public class ByteProperty : FPropertyTagType<byte>
        {
            public ByteProperty(byte value)
            {
                Value = value;
            }
        }
        
        public class DoubleProperty : FPropertyTagType<double>
        {
            public DoubleProperty(double value)
            {
                Value = value;
            }
        }
        
        public class EnumProperty : FPropertyTagType<FName>
        {
            public EnumProperty(FName value)
            {
                Value = value;
            }
        }

        public class DelegateProperty : FPropertyTagType<FName>
        {
            public readonly int Num;

            public DelegateProperty(int num, FName value)
            {
                Num = num;
                Value = value;
            }
        }

        public class ArrayProperty : FPropertyTagType<UScriptArray>
        {
            public ArrayProperty(UScriptArray value)
            {
                Value = value;
            }
        }

        public class StructProperty : FPropertyTagType<UScriptStruct>
        {
            public StructProperty(UScriptStruct value)
            {
                Value = value;
            }
        }

        public class TextProperty : FPropertyTagType<FText>
        {
            public TextProperty(FText value)
            {
                Value = value;
            }
        }

        public class SoftObjectProperty : FPropertyTagType<FSoftObjectPath>
        {
            public SoftObjectProperty(FSoftObjectPath value)
            {
                Value = value;
            }
        }

        public class MapProperty : FPropertyTagType<UScriptMap>
        {
            public MapProperty(UScriptMap value)
            {
                Value = value;
            }
        }

        public class SetProperty : FPropertyTagType<UScriptArray>
        {
            public SetProperty(UScriptArray value)
            {
                Value = value;
            }
        }
    }
}
