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
    private const uint CEXT_MAGIC = 0x54584543;
    private const uint PPTH_MAGIC = 0x48545050;
    private const uint ENVP_MAGIC = 0x50564E45;

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
                var _ = Ar.Read(data, 0, (int) compSize);
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
                using var decompressor = new Decompressor();
                data = decompressor.Unwrap(Ar.ReadBytes((int) compSize), (int) decompSize).ToArray();
                break;
            }
            default:
                throw new ParserException($"Invalid compression method {CompressionMethod}");
        }

        Ar.Dispose();
        Ar = new FUsmapReader(new FByteArchive(Ar.Name, data), Ar.Version);
        var nameSize = Ar.Read<uint>();
        var nameLut = new List<string>((int) nameSize);
        for (var i = 0; i < nameSize; i++)
        {
            var nameLength = Ar.Read<byte>();
            nameLut.Add(Ar.ReadStringUnsafe(nameLength));
        }

        var enumCount = Ar.Read<uint>();
        var enums = new List<(string, string?, List<(long, string)>)>((int) enumCount);

        var mappings = new TypeMappings();
        for (var i = 0; i < enumCount; i++)
        {
            var enumName = Ar.ReadName(nameLut)!;

            var enumNamesSize = Ar.Read<byte>();
            var enumNames = new List<(long, string)>(enumNamesSize);
            for (var j = 0; j < enumNamesSize; j++)
            {
                var value = Ar.ReadName(nameLut)!;
                enumNames.Add((j, value));
            }

            enums.Add((enumName, null, enumNames));
        }

        var structCount = Ar.Read<uint>();
        var structs = new List<Struct>();

        for (var i = 0; i < structCount; i++)
        {
            structs.Add(UsmapProperties.ParseStruct(mappings, Ar, nameLut));
        }

        if (Ar.Length - Ar.Position > 5) {
            if (Ar.Read<uint>() == CEXT_MAGIC) {

                var version = Ar.ReadByte();
                if (version > 0) {
                    return;
                }

                var extensionCount = Ar.Read<int>();
                for (var i = 0; i < extensionCount; ++i) {
                    var extMagic = Ar.Read<uint>();
                    var extensionSize = Ar.Read<int>();
                    using var ExtAr = new FUsmapReader(new FByteArchive(Ar.Name, Ar.ReadBytes(extensionSize)), Ar.Version);

                    if (extMagic == PPTH_MAGIC && ExtAr.ReadByte() == 0) {
                        var enumPathCount = ExtAr.Read<int>();
                        for (var index = 0; index < enumPathCount; ++index) {
                            var name = ExtAr.ReadName(nameLut)!;
                            enums[index] = (enums[index].Item1, name, enums[index].Item3);
                        }

                        var structPathCount = ExtAr.Read<int>();
                        for (var index = 0; index < structPathCount; ++index) {
                            var name = ExtAr.ReadName(nameLut)!;
                            structs[index].Module = name;
                        }
                    } else if (extMagic == ENVP_MAGIC && ExtAr.ReadByte() == 0) {
                        enumCount = ExtAr.Read<uint>();
                        for (var index = 0; index < enumCount; ++index) {
                            var valueCount = ExtAr.Read<int>();
                            var enumValues = new List<(long, string)>(valueCount);
                            for (var j = 0; j < valueCount; ++j) {
                                var name = ExtAr.ReadName(nameLut)!;
                                var id = ExtAr.Read<long>();
                                enumValues.Add((id, name));
                            }

                            enums[index] = (enums[index].Item1, enums[index].Item2, enumValues);
                        }
                    }
                }
            }
        }

        foreach (var enumClass in enums) {
            mappings.Enums[enumClass.Item1] = enumClass.Item3;
            mappings.Enums[(enumClass.Item2 ?? "") + "/" + enumClass.Item1] = enumClass.Item3;
        }

        foreach (var structClass in structs) {
            mappings.Types[structClass.Name] = structClass;
            mappings.Types[(structClass.Module ?? "") + "/" + structClass.Name] = structClass;
        }

        Mappings = mappings;
        Ar.Dispose();
    }
}
