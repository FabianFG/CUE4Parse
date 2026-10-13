using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Assets.Exports.Rig.RigLogic;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public abstract class UDNAObject : UObject, IDnaAsset
{
    public DNAVersion? Version { get; protected set; }
    public Dictionary<string, IRawBase>? Layers { get; protected set; }
    public Lazy<byte[]>? DNAData { get; protected set; }
    public string? DnaFileName { get; protected set; }
    public RigLogicSnapshot? Snapshot { get; protected set; }

    public int GetRawControlCount() => GetRawControlNames().Length;

    public string[] GetRawControlNames() => DnaAssetQueries.GetRawControlNames(Layers);

    public string GetRawControlName(int index)
    {
        var names = GetRawControlNames();
        return index >= names.Length
            ? throw new IndexOutOfRangeException($"Index {index} is greater than total raw control count")
            : names[index];
    }

    public int GetJointCount() => GetJointNames().Length;

    public string[] GetJointNames() => DnaAssetQueries.GetJointNames(Layers);

    public string GetJointName(int index)
    {
        var names = GetJointNames();
        return index >= names.Length
            ? throw new IndexOutOfRangeException($"Index {index} is greater than total joint count")
            : names[index];
    }

    public RawBehavior? GetBehavior() => DnaAssetQueries.GetBehavior(Layers);

    protected void ApplyDocument(DnaBinaryDocument doc, byte[] dnaBytes)
    {
        Version = doc.Version;
        Layers = doc.Layers;
        DNAData = new Lazy<byte[]>(() => dnaBytes);
    }

    protected static void ParseDnaStream(FAssetArchive Ar, long limitPos, UDNAObject dest, bool parseTrailingSnapshot)
    {
        var dnaStart = Ar.Position;
        var endianAr = new FArchiveBigEndian(Ar);
        var doc = DnaBinaryParser.Parse(endianAr, dnaStart);
        var dnaBytes = Ar.ReadBytesAt(dnaStart, (int) doc.ByteLength);
        dest.ApplyDocument(doc, dnaBytes);
        Ar.Position = dnaStart + doc.ByteLength;

        if (parseTrailingSnapshot && Ar.Position < limitPos)
        {
            var remaining = (int) (limitPos - Ar.Position);
            var snapshotBytes = Ar.ReadBytes(remaining);
            dest.Snapshot = RigLogicSnapshot.Read(snapshotBytes, requireFullConsume: false);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Version));
        serializer.Serialize(writer, Version);

        if (Layers is not null && DnaAssetQueries.TryGet(Layers, "desc", out RawDescriptor descriptor))
        {
            writer.WritePropertyName("Descriptor");
            serializer.Serialize(writer, descriptor);
        }
    }
}

public class UDNAAsset : UDNAObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DnaFileName = GetOrDefault(nameof(DnaFileName), GetOrDefault<string>("DnaFileName_DEPRECATED"));

        var customVer = FDNAAssetCustomVersion.Get(Ar);
        if (customVer < FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded) return;

        if (customVer == FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            ParseDnaStream(Ar, validPos, this, parseTrailingSnapshot: false);
            if (Ar.Position < validPos && DnaBinaryParser.LooksLikeDna(Ar))
                ParseDnaStream(Ar, validPos, this, parseTrailingSnapshot: false);
            return;
        }

        ParseDnaStream(Ar, validPos, this, parseTrailingSnapshot: false);
    }
}

public class UDNA : UDNAObject
{
    public FDNAConfig DNAConfig;
    public bool bKeepDNAAfterInitialization;
    public bool bUseOptimizedCooking = true;
    public FRigLogicConfiguration RigLogicConfiguration;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        DNAConfig = GetOrDefault<FDNAConfig>(nameof(DNAConfig));
        bKeepDNAAfterInitialization = GetOrDefault<bool>(nameof(bKeepDNAAfterInitialization));
        bUseOptimizedCooking = GetOrDefault(nameof(bUseOptimizedCooking), true);
        RigLogicConfiguration = GetOrDefault<FRigLogicConfiguration>(nameof(RigLogicConfiguration));
        

        if (Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
            return;

        var customVer = FDNAAssetCustomVersion.Get(Ar);
        if (customVer < FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
            return;

        if (customVer == FDNAAssetCustomVersion.Type.BeforeCustomVersionWasAdded)
        {
            ParseDnaStream(Ar, validPos, this, parseTrailingSnapshot: false);
            if (Ar.Position < validPos && DnaBinaryParser.LooksLikeDna(Ar))
                ParseDnaStream(Ar, validPos, this, parseTrailingSnapshot: false);
            return;
        }

        var optimized = bUseOptimizedCooking
                        && Ar.IsLoadingFromCookedPackage
                        && customVer >= FDNAAssetCustomVersion.Type.IntroduceOptimizedSerializationDuringCooking;

        if (optimized)
        {
            if (!Ar.ReadBoolean())
                return;
            ParseDnaStream(Ar, validPos, this, parseTrailingSnapshot: true);
            return;
        }

        ParseDnaStream(Ar, validPos, this, parseTrailingSnapshot: Ar.Position < validPos && !DnaBinaryParser.LooksLikeDna(Ar));
    }
}

public class UDNAAssetUserData : UObject
{
    public UDNA? DNAAsset;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DNAAsset = GetOrDefault<UDNA>(nameof(DNAAsset));
    }
}
