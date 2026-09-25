using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CompMethod = CUE4Parse.Compression.CompressionMethod;

namespace CUE4Parse.MappingsProvider.Usmap;

public class UsmapParser
{
    private const ushort FileMagic = 0x30C4;
    public readonly TypeMappings? Mappings;
    public readonly EUsmapCompressionMethod CompressionMethod;
    public readonly EUsmapVersion Version;
    public readonly bool HasVersioning;
    public readonly FEngineVersion? EngineVersion;
    public readonly FPackageFileVersion PackageVersion;
    public readonly FCustomVersionContainer CustomVersions;
    public readonly uint NetCL;
    public readonly FUsmapMetadata? Metadata;

    public UsmapParser(string path, string name = "An unnamed usmap", StringComparer? comparer = null) : this(File.OpenRead(path), name, comparer) { }
    public UsmapParser(Stream data, string name = "An unnamed usmap", StringComparer? comparer = null) : this(new FStreamArchive(name, data), comparer) { }
    public UsmapParser(byte[] data, string name = "An unnamed usmap", StringComparer? comparer = null) : this(new FByteArchive(name, data), comparer) { }

    public UsmapParser(FArchive archive, StringComparer? comparer = null)
    {
        if (archive.Length < 2)
            throw new ParserException("Usmap is empty");

        var magic = archive.Read<ushort>();
        if (magic != FileMagic)
            throw new ParserException("Usmap has invalid magic");

        Version = archive.Read<EUsmapVersion>();
        if (Version > EUsmapVersion.Latest)
            throw new ParserException($"Usmap has invalid version ({(byte) Version})");

        Metadata = Version >= EUsmapVersion.ExtendedMetadata ? new FUsmapMetadata(archive) : null;

        HasVersioning = Version >= EUsmapVersion.PackageVersioning && archive.ReadBoolean();
        if (HasVersioning)
        {
            if (Version >= EUsmapVersion.EngineVersioning)
            {
                EngineVersion = new FEngineVersion(archive);
            }

            PackageVersion = new FPackageFileVersion(archive.Read<int>(), archive.Read<int>());
            CustomVersions = new FCustomVersionContainer(archive);
            NetCL = archive.Read<uint>();
        }
        else
        {
            PackageVersion = default;
            CustomVersions = new FCustomVersionContainer();
            NetCL = 0;
        }

        CompressionMethod = archive.Read<EUsmapCompressionMethod>();

        var compSize = archive.Read<uint>();
        var decompSize = archive.Read<uint>();

        var data = new byte[decompSize];

        if (CompressionMethod == EUsmapCompressionMethod.None)
        {
            if (compSize != decompSize)
                throw new ParserException("No compression: Compression size must be equal to decompression size");
            archive.ReadExactly(data, 0, (int) compSize);
        }
        else
        {
            var method = CompressionMethod switch
            {
                EUsmapCompressionMethod.Oodle => CompMethod.Oodle,
                EUsmapCompressionMethod.Brotli => CompMethod.Brotli,
                EUsmapCompressionMethod.ZStandard => CompMethod.Zstd,
                _ => CompMethod.Unknown
            };
            var compressed = archive.ReadBytes((int) compSize);
            Compression.Compression.Decompress(compressed, data, method, archive);
        }

        var Ar = new FUsmapReader(new FByteArchive(archive.Name, data), Version);

        var enumCount = Ar.Read<uint>();
        var enums = new Dictionary<string, Dictionary<long, string>>((int) enumCount);
        for (var i = 0; i < enumCount; i++)
        {
            if (Ar.Version >= EUsmapVersion.ExtendedMetadata)
                _ = Ar.ReadName(); // owner package name (or null)
            var enumName = Ar.ReadName()!;

            var enumNamesSize = Ar.Version >= EUsmapVersion.LargeEnums ? Ar.Read<ushort>() : Ar.Read<byte>();
            var enumNames = new Dictionary<long, string>(enumNamesSize);

            if (Ar.Version >= EUsmapVersion.ExplicitEnumValues)
            {
                for (var j = 0; j < enumNamesSize; j++)
                {
                    var value = Ar.Read<ulong>();
                    var name = Ar.ReadName()!;
                    enumNames[(long) value] = name;
                }
            }
            else
            {
                for (var j = 0; j < enumNamesSize; j++)
                {
                    var value = Ar.ReadName()!;
                    enumNames[j] = value;
                }
            }

            // Some companies man... Their duplicated enums, even with different values, have to be ignored.
            enums.TryAdd(enumName, enumNames);
        }

        var structCount = Ar.Read<uint>();
        var structs = new Dictionary<string, Struct>(comparer ?? StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(structs, enums);

        for (var i = 0; i < structCount; i++)
        {
            var s = UsmapProperties.ParseStruct(mappings, Ar);
            structs[s.Name] = s;
        }

        Mappings = mappings;
        archive.Dispose();
    }
}