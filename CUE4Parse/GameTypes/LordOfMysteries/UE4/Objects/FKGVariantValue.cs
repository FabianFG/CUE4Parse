using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.LordOfMysteries.UE4.Objects;

public class FKGVariantValue : FStructFallback
{
    public FKGVariantValue(FAssetArchive Ar) : base()
    {
        // maybe count, could be FFieldPath
        var varianttype = Ar.Read<int>();
        var name = Ar.ReadFName();
        var strukt = new FPackageIndex(Ar);
        if (!strukt.TryLoad<UStruct>(out var struc))
            return;

        var type = struc.Name;
        Struct? propMappings = null;
        if (struc is UScriptClass)
            Ar.Owner!.Mappings?.TryGetType(
                type,
                strukt.ResolvedObject?.GetPathName() ?? (struc as UScriptClass)?.FullTypeIdentifier,
                out propMappings!);
        else
            propMappings = new SerializedStruct(Ar.Owner!.Mappings, struc);

        var propInfo = propMappings?.Properties.FirstOrDefault(x => x.Value.Name.Equals(name.Text, StringComparison.OrdinalIgnoreCase));
        
        if (propInfo?.Value is not null)
        {
            var prop = new FPropertyTag(Ar, propInfo.Value.Value, ReadType.NORMAL);
            Properties.Add(prop);
        }
    }
}
