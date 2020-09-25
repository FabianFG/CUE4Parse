using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Objects
{
    public enum ReadType
    {
        NORMAL,
        MAP,
        ARRAY
    }

    public abstract class FPropertyTagType
    {
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
                case "ArrayProperty":
                    return tagData switch
                    {
                        FPropertyTagData.ArrayProperty arrayProperty => new ArrayProperty(new UScriptArray(Ar, arrayProperty.InnerType.Text)),
                        _ => null,
                    };
                case "StructProperty": return new StructProperty(new UScriptStruct(Ar, (tagData as FPropertyTagData.StructProperty)?.StructName.Text));
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
                case "Int8Property": return new Int8Property(Ar.Read<byte>());
                case "MapProperty":
                    return tagData switch
                    {
                        FPropertyTagData.MapProperty mapProperty => new MapProperty(new UScriptMap(Ar, mapProperty)),
                        _ => null,
                    };
                case "SetProperty":
                    return tagData switch
                    {
                        FPropertyTagData.SetProperty setProperty => new SetProperty(new UScriptArray(Ar, setProperty.InnerType.Text)),
                        _ => null,
                    };
                case "EnumProperty":
                    if (type == ReadType.NORMAL && (tagData as FPropertyTagData.EnumProperty)?.EnumName.IsNone == true)
                        return new EnumProperty(new FName());
                    else
                        return new EnumProperty(Ar.ReadFName());
                default:
#if DEBUG
                    Console.WriteLine($"Couldn't read property type {propertyType} at {Ar.Position}");         
#endif
                    return null;
            }
            return null;
        }

        public class BoolProperty : FPropertyTagType
        {
            public readonly bool Value;

            public BoolProperty(bool value)
            {
                Value = value;
            }
        }
        
        public class ObjectProperty : FPropertyTagType
        {
            public readonly FPackageIndex Value;

            public ObjectProperty(FPackageIndex value)
            {
                Value = value;
            }
        }
        
        public class InterfaceProperty : FPropertyTagType
        {
            public readonly UInterfaceProperty Value;

            public InterfaceProperty(UInterfaceProperty value)
            {
                Value = value;
            }
        }
        
        public class FloatProperty : FPropertyTagType
        {
            public readonly float Value;

            public FloatProperty(float value)
            {
                Value = value;
            }
        }

        public class StrProperty : FPropertyTagType
        {
            public readonly string Value;

            public StrProperty(string value)
            {
                Value = value;
            }
        }
        
        public class NameProperty : FPropertyTagType
        {
            public readonly FName Value;

            public NameProperty(FName value)
            {
                Value = value;
            }
        }
        
        public class IntProperty : FPropertyTagType
        {
            public readonly int Value;

            public IntProperty(int value)
            {
                Value = value;
            }
        }
        
        public class UInt16Property : FPropertyTagType
        {
            public readonly ushort Value;

            public UInt16Property(ushort value)
            {
                Value = value;
            }
        }
        
        public class UInt32Property : FPropertyTagType
        {
            public readonly uint Value;

            public UInt32Property(uint value)
            {
                Value = value;
            }
        }
        
        public class UInt64Property : FPropertyTagType
        {
            public readonly ulong Value;

            public UInt64Property(ulong value)
            {
                Value = value;
            }
        }
        
        public class Int64Property : FPropertyTagType
        {
            public readonly long Value;

            public Int64Property(long value)
            {
                Value = value;
            }
        }
        
        public class Int16Property : FPropertyTagType
        {
            public readonly short Value;

            public Int16Property(short value)
            {
                Value = value;
            }
        }
        
        public class Int8Property : FPropertyTagType
        {
            public readonly byte Value;

            public Int8Property(byte value)
            {
                Value = value;
            }
        }
        
        public class ByteProperty : FPropertyTagType
        {
            public readonly byte Value;

            public ByteProperty(byte value)
            {
                Value = value;
            }
        }
        
        public class DoubleProperty : FPropertyTagType
        {
            public readonly double Value;

            public DoubleProperty(double value)
            {
                Value = value;
            }
        }
        
        public class EnumProperty : FPropertyTagType
        {
            public readonly FName Value;

            public EnumProperty(FName value)
            {
                Value = value;
            }
        }

        public class DelegateProperty : FPropertyTagType
        {
            public readonly int Value;
            public readonly FName Name;

            public DelegateProperty(int value, FName name)
            {
                Value = value;
                Name = name;
            }
        }

        public class ArrayProperty : FPropertyTagType
        {
            public readonly UScriptArray Value;

            public ArrayProperty(UScriptArray value)
            {
                Value = value;
            }
        }

        public class StructProperty : FPropertyTagType
        {
            public readonly UScriptStruct Value;

            public StructProperty(UScriptStruct value)
            {
                Value = value;
            }
        }

        public class TextProperty : FPropertyTagType
        {
            public readonly FText Value;

            public TextProperty(FText value)
            {
                Value = value;
            }
        }

        public class SoftObjectProperty : FPropertyTagType
        {
            public readonly FSoftObjectPath Value;

            public SoftObjectProperty(FSoftObjectPath value)
            {
                Value = value;
            }
        }

        public class MapProperty : FPropertyTagType
        {
            public readonly UScriptMap Value;

            public MapProperty(UScriptMap value)
            {
                Value = value;
            }
        }

        public class SetProperty : FPropertyTagType
        {
            public readonly UScriptArray Value;

            public SetProperty(UScriptArray value)
            {
                Value = value;
            }
        }
    }
}
