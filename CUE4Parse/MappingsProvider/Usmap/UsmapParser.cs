using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using ZstdSharp;

namespace CUE4Parse.MappingsProvider.Usmap;

public class UsmapParser
{
    private const ushort FileMagic = 0x30C4;
    public readonly TypeMappings? Mappings;
    public readonly EUsmapCompressionMethod CompressionMethod;
    public readonly EUsmapVersion Version;
    public readonly FPackageFileVersion PackageVersion;
    public readonly FCustomVersionContainer CustomVersions;
    public readonly uint NetCL;

    public UsmapParser(string path, string name = "An unnamed usmap") : this(File.OpenRead(path), name) { }
    public UsmapParser(Stream data, string name = "An unnamed usmap") : this(new FStreamArchive(name, data)) { }
    public UsmapParser(byte[] data, string name = "An unnamed usmap") : this(new FByteArchive(name, data)) { }

    public UsmapParser(FArchive archive)
    {
        var magic = archive.Read<ushort>();
        if (magic != FileMagic)
            throw new ParserException("Usmap has invalid magic");

        Version = archive.Read<EUsmapVersion>();
        if (Version > EUsmapVersion.Latest)
            throw new ParserException($"Usmap has invalid version ({(byte) Version})");

        var Ar = new FUsmapReader(archive, Version);

        var bHasVersioning = Ar.Version >= EUsmapVersion.PackageVersioning && Ar.ReadBoolean();
        if (bHasVersioning)
        {
            PackageVersion = Ar.Read<FPackageFileVersion>();
            CustomVersions = new FCustomVersionContainer(Ar);
            NetCL = Ar.Read<uint>();
        }
        else
        {
            PackageVersion = default;
            CustomVersions = new FCustomVersionContainer();
            NetCL = 0;
        }

        CompressionMethod = Ar.Read<EUsmapCompressionMethod>();

        var compSize = Ar.Read<uint>();
        var decompSize = Ar.Read<uint>();

        var data = new byte[decompSize];
        switch (CompressionMethod)
        {
            case EUsmapCompressionMethod.None:
            {
                if (compSize != decompSize)
                    throw new ParserException("No compression: Compression size must be equal to decompression size");
                _ = Ar.Read(data, 0, (int) compSize);
                break;
            }
            case EUsmapCompressionMethod.Oodle:
            {
                Oodle.Decompress(Ar.ReadBytes((int) compSize), 0, (int) compSize, data, 0, (int) decompSize);
                break;
            }
            case EUsmapCompressionMethod.Brotli:
            {
                using var decoder = new BrotliDecoder();
                decoder.Decompress(Ar.ReadBytes((int) compSize), data, out _, out _);
                break;
            }
            case EUsmapCompressionMethod.ZStandard:
            {
                var decompressor = new Decompressor();
                data = decompressor.Unwrap(Ar.ReadBytes((int) compSize), (int) decompSize).ToArray();
                break;
            }
            default:
                throw new ParserException($"Invalid compression method {CompressionMethod}");
        }

        Ar = new FUsmapReader(new FByteArchive(Ar.Name, data), Ar.Version);
        var nameSize = Ar.Read<uint>();
        var nameLut = new List<string>((int) nameSize);
        for (var i = 0; i < nameSize; i++)
        {
            var nameLength = Ar.Version >= EUsmapVersion.LongFName ? Ar.Read<ushort>() : Ar.Read<byte>();
            nameLut.Add(Ar.ReadStringUnsafe(nameLength));
        }

        var enumCount = Ar.Read<uint>();
        var enums = new Dictionary<string, Dictionary<int, string>>((int) enumCount);
        for (var i = 0; i < enumCount; i++)
        {
            var enumName = Ar.ReadName(nameLut)!;

            var enumNamesSize = Ar.Version >= EUsmapVersion.LargeEnums ? Ar.Read<ushort>() : Ar.Read<byte>();
            var enumNames = new Dictionary<int, string>(enumNamesSize);
            for (var j = 0; j < enumNamesSize; j++)
            {
                var value = Ar.ReadName(nameLut)!;
                enumNames[j] = value;
            }

            enums.Add(enumName, enumNames);
        }

        var structCount = Ar.Read<uint>();
        var structs = new Dictionary<string, Struct>();

        var mappings = new TypeMappings(structs, enums);

        for (var i = 0; i < structCount; i++)
        {
            var s = UsmapProperties.ParseStruct(mappings, Ar, nameLut);
            structs[s.Name] = s;
        }

        Mappings = mappings;
    }
}
