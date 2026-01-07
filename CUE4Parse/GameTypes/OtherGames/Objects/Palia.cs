using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public enum EVAL_PremiumItemType : byte
{
    Normal = 0,
    Premium = 1,
}

public record class FPerBodyTypeConfig(EVAL_PremiumItemType ItemType, FStructFallback Config);
public class UVAL_PremiumItemAsset : UPrimaryDataAsset
{
    public FPerBodyTypeConfig[] PerBodyTypeConfig = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Position >= validPos)
            return;
        var type = ExportType.StartsWith("VAL_CharacterCustomizationItem_") && ExportType.EndsWith("Skin")
            ? "VAL_CharacterCustomizationItem_ToolSkin_SkeletalMesh_PerBodyTypeConfig"
            : ExportType + "_PerBodyTypeConfig";
        PerBodyTypeConfig = Ar.ReadArray(() => new FPerBodyTypeConfig(Ar.Read<EVAL_PremiumItemType>(), new FStructFallback(Ar, type)));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(PerBodyTypeConfig));
        serializer.Serialize(writer, PerBodyTypeConfig);
    }
}

public class FVAL_CharacterCustomizationVariantOptionsArray : IUStruct
{
    public FPackageIndex OptionStruct;
    public IUStruct[] Options = [];
    public FVAL_CharacterCustomizationVariantOptionsArray(FAssetArchive Ar)
    {
        OptionStruct = new FPackageIndex(Ar);
        if (OptionStruct.IsNull) return;

        try
        {
            var structName = OptionStruct.ResolvedObject is { } obj ? obj.Name.ToString() : null;
            if (OptionStruct.TryLoad<UStruct>(out var struc) || structName != null)
            {
                Options = Ar.ReadArray(() => new FScriptStruct(Ar, structName, struc, ReadType.NORMAL).StructType);
            }
            else
            {
                Log.Warning("Failed to read FVAL_CharacterCustomizationVariantOptionsArray of type {0}, skipping it", OptionStruct.ResolvedObject?.GetFullName());
            }
        }
        catch (ParserException e)
        {
            Log.Warning(e, "Failed to read FVAL_CharacterCustomizationVariantOptionsArray of type {0}, skipping it", OptionStruct.ResolvedObject?.GetFullName());
        }
    }
}
