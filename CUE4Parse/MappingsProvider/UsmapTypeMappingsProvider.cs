using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider
{


    public abstract class UsmapTypeMappingsProvider : AbstractTypeMappingsProvider
    {
        public override Dictionary<string, TypeMappings> MappingsByGame { get; protected set; } = new Dictionary<string, TypeMappings>();

        protected void AddUsmap(byte[] usmap, string game, string name = "An unnamed usmap")
        {
            MappingsByGame[game] = UsmapParser.Parse(usmap, name);
        }
        
    }


    public static class UsmapParser
    {

        public const ushort FileMagic = 0x30C4;
        
        public enum Version : byte {
            INITIAL,

            LATEST_PLUS_ONE,
            LATEST = LATEST_PLUS_ONE - 1
        }
        
        public enum ECompressionMethod : byte {
            None,
            Oodle,
            Brotli,

            Unknown = 0xFF
        }

        public static TypeMappings Parse(Stream data, string name = "An unnamed usmap")
        {
            return Parse(new FStreamArchive(name, data));
        }

        public static TypeMappings Parse(byte[] data, string name = "An unnamed usmap")
        {
            return Parse(new FByteArchive(name, data));
        }
        
        public static TypeMappings Parse(FArchive Ar)
        {
            var magic = Ar.Read<ushort>();
            if (magic != FileMagic)
                throw new ParserException(".usmap file has an invalid magic constant");

            var version = Ar.Read<Version>();
            if (version < 0 || version > Version.LATEST)
                throw new ParserException($".usmap has an invalid version {(byte) version}");

            var compression = Ar.Read<ECompressionMethod>();

            var compSize = Ar.Read<uint>();
            var decompSize = Ar.Read<uint>();
            
            var data = new byte[decompSize];
            switch (compression)
            {
                case ECompressionMethod.None:
                    if (compSize != decompSize)
                        throw new ParserException("No compression: Compression size must be equal to decompression size");
                    Ar.Read(data, 0, (int) compSize);
                    break;
                case ECompressionMethod.Oodle:
                    Oodle.Decompress(Ar.ReadBytes((int) compSize), 0, (int) compSize, data, 0, (int) decompSize);
                    break;
                case ECompressionMethod.Brotli:
                    throw new NotImplementedException();
                default:
                    throw new ParserException($"Invalid compression method {compression}");
            }

            Ar = new FByteArchive(Ar.Name, data);
            var nameSize = Ar.Read<uint>();
            var nameLut = new List<String>((int) nameSize);
            for (int i = 0; i < nameSize; i++)
            {
                var nameLength = Ar.Read<byte>();
                nameLut.Add(ReadStringUnsafe(Ar, nameLength));
            }

            var enumCount = Ar.Read<uint>();
            var enums = new Dictionary<string, Dictionary<int, string>>((int) enumCount);
            for (int i = 0; i < enumCount; i++)
            {
                var enumName = Ar.ReadName(nameLut)!;

                var enumNamesSize = Ar.Read<byte>();
                var enumNames = new Dictionary<int, string>(enumNamesSize);
                for (int j = 0; j < enumNamesSize; j++)
                {
                    var value = Ar.ReadName(nameLut)!;
                    enumNames[j] = value;
                }
                
                enums.Add(enumName, enumNames);
            }

            var structCount = Ar.Read<uint>();
            var structs = new Dictionary<string, Struct>();
            
            var mappings = new TypeMappings(structs, enums);
            
            for (int i = 0; i < structCount; i++)
            {
                var s = ParseStruct(mappings, Ar, nameLut);
                structs[s.Name] = s;
            }
            
            return mappings;
        }

        private static Struct ParseStruct(TypeMappings context, FArchive Ar, IReadOnlyList<string> nameLut)
        {
            var name = Ar.ReadName(nameLut)!;
            var superType = Ar.ReadName(nameLut);

            var propertyCount = Ar.Read<ushort>();
            var serializablePropertyCount = Ar.Read<ushort>();
            var properties = new Dictionary<int, PropertyInfo>();
            for (int i = 0; i < serializablePropertyCount; i++)
            {
                var propInfo = ParsePropertyInfo(Ar, nameLut);
                properties[propInfo.Index] = propInfo;
            }
            return new Struct(context, name, superType, properties, propertyCount);
        }

        private static PropertyInfo ParsePropertyInfo(FArchive Ar, IReadOnlyList<string> nameLut)
        {
            var index = Ar.Read<ushort>();
            var arrayDim = Ar.Read<byte>();
            var name = Ar.ReadName(nameLut)!;
            var type = ParsePropertyType(Ar, nameLut);
            return new PropertyInfo(index, name, type, arrayDim);
        }

        private static PropertyType ParsePropertyType(FArchive Ar, IReadOnlyList<string> nameLut)
        {
            var typeEnum = Ar.Read<EPropertyType>();
            var type = Enum.GetName(typeof(EPropertyType), typeEnum)!;
            string? structType = null;
            PropertyType innerType = null;
            PropertyType valueType = null;
            string? enumName = null;
            bool? isEnumAsByte = null;

            switch (typeEnum)
            {
                case EPropertyType.EnumProperty:
                    innerType = ParsePropertyType(Ar, nameLut);
                    enumName = Ar.ReadName(nameLut);
                    break;
                case EPropertyType.StructProperty:
                    structType = Ar.ReadName(nameLut);
                    break;
                case EPropertyType.SetProperty:
                case EPropertyType.ArrayProperty:
                    innerType = ParsePropertyType(Ar, nameLut);
                    break;
                case EPropertyType.MapProperty:
                    innerType = ParsePropertyType(Ar, nameLut);
                    valueType = ParsePropertyType(Ar, nameLut);
                    break;
            }

            return new PropertyType(type, structType, innerType, valueType, enumName, isEnumAsByte);
        }

        private const int InvalidNameIndex = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? ReadName(this FArchive Ar, IReadOnlyList<string> nameLut)
        {
            var idx = Ar.ReadNameEntry();
            return idx != InvalidNameIndex ? nameLut[idx] : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadNameEntry(this FArchive Ar)
        {
            return Ar.Read<int>();
        }

        private static unsafe string ReadStringUnsafe(FArchive Ar, int nameLength)
        {
            var nameBytes = stackalloc byte[nameLength];
            Ar.Read(nameBytes, nameLength);
            return new string((sbyte*) nameBytes, 0, nameLength);
        }
    }
    
    enum EPropertyType : byte {
        ByteProperty,
        BoolProperty,
        IntProperty,
        FloatProperty,
        ObjectProperty,
        NameProperty,
        DelegateProperty,
        DoubleProperty,
        ArrayProperty,
        StructProperty,
        StrProperty,
        TextProperty,
        InterfaceProperty,
        MulticastDelegateProperty,
        WeakObjectProperty, //
        LazyObjectProperty, // When deserialized, these 3 properties will be SoftObjects
        AssetObjectProperty, //
        SoftObjectProperty,
        UInt64Property,
        UInt32Property,
        UInt16Property,
        Int64Property,
        Int16Property,
        Int8Property,
        MapProperty,
        SetProperty,
        EnumProperty,
        FieldPathProperty,

        Unknown = 0xFF
    };
}