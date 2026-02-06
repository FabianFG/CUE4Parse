using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Internationalization;

public class UStringTable : UObject
{
    private static readonly ConcurrentDictionary<string, UStringTable> _cache = new();

    public FStringTable StringTable { get; private set; }

    internal static bool TryGet(IFileProvider provider, string tableId, [MaybeNullWhen(false)] out UStringTable table)
    {
        if (_cache.TryGetValue(tableId, out table))
            return true;

        if (provider.TryLoadPackageObject(tableId, out table))
        {
            _cache.TryAdd(tableId, table);
            return true;
        }

        table = null;
        return false;
    }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        StringTable = new FStringTable(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("StringTable");
        serializer.Serialize(writer, StringTable);
    }
}
