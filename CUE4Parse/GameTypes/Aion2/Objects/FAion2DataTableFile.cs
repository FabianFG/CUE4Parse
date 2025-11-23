using System;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using Serilog;

namespace CUE4Parse.GameTypes.Aion2.Objects;

public class FAion2DataTableFile : FAion2DataFile
{
    public FAion2DataTableFile(GameFile file, IFileProvider provider)
    {
        var data = file.SafeRead();
        ArgumentNullException.ThrowIfNull(data);

        if (!file.Directory.EndsWith("Data/Table", StringComparison.OrdinalIgnoreCase)) return;

        using var Ar = new FAion2DatFileArchive(data, provider.Versions);
        Version = Ar.Read<int>();
        Ids = Ar.ReadFString().Split(",");

        try
        {
            var name = file.NameWithoutExtension + "TableItem";
            name = name switch
            {
                "AgitDecoItemTableItem" => "PersonalAgitDecoItemTableItem",
                "AgitDecoSlotTableItem" => "PersonalAgitDecoSlotTableItem",
                "EnvObjDataTableItem" => "EnvObjTableItem",
                "KiskDataTableItem" => "KiskTableItem",
                "MonsterPartsDataTableItem" => "PartsDataTableItem",
                "PeriodItemCollectionTableItem" => "PeriodCollectionTableItem",
                "PeriodItemCollectionListTableItem" => "PeriodCollectionListTableItem",
                "RandomShopTableItem" => "RandomShopCategoryTableItem",
                "ResourcesTableItem" => "ResourceTableItem",
                "RewardDataTableItem" => "RewardTableItem",
                "RewardLevelScaleDataTableItem" => "RewardLevelScaleTableItem",
                "RewardDataMonsterCubeTableItem" => "MonsterCubeTableItem",
                "SkillAbnormalTableItem" => "AbnormalTableItem",
                "SkillAbnormalEffectTableItem" => "AbnormalEffectTableItem",
                "SkillAbnormalEffectLvTableItem" => "AbnormalEffectLevelTableItem",
                "SkillAbnormalEffectTypeTableItem" => "AbnormalEffectTypeTableItem",
                "SkillAbnormalOverlapFxTableItem" => "AbnormalOverlapFxTableItem",
                "SkillAbnormalPropertyTableItem" => "AbnormalPropertyTableItem",
                "SkillAcquireDataTableItem" => "SkillAcquireTableItem",
                "SkillProjectileTableItem" => "ProjectileTableItem",
                "StringTableItem" => "AionStringTableItem",
                "ServerMessageTableItem" => "ServerBroadcastDataTableItem",
                "VehicleListTableItem" => "VehicleDataTableItem",
                "WaveStepDataTableItem" => "WaveStepTableItem",
                "CVResource_StartUpTableItem" => "CVResourceTableItem",
                "InputAction_StartUpTableItem" => "InputActionTableItem",
                "InputEvent_StartUpTableItem" => "InputEventTableItem",
                "InputKeyMapping_StartUpTableItem" => "InputKeyMappingTableItem",
                "PackageList_StartUpTableItem" => "PackageListTableItem",
                "ResourcePak_StartUpTableItem" => "ResourcePakTableItem",
                "String_StartUpTableItem" => "AionStringTableItem",
                _ => name
            };

            var tagData = new FPropertyTagData()
            {
                Name = file.NameWithoutExtension + "Table",
                Type = "ArrayProperty",
                InnerType = "StructProperty",
                InnerTypeData = new FPropertyTagData(name),
            };

            var tag = new FPropertyTag
            {
                Name = "Data",
                PropertyType = "ArrayProperty",
                Tag = FAion2PropertyReader.ReadPropertyTagType(Ar, provider.MappingsContainer.MappingsForGame, "ArrayProperty", tagData, false, ReadType.ARRAY),
                TagData = tagData,
            };

            Properties.Add(tag);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to parse FAion2DatFile FAion2DataTableFile {0}", file.Name);
        }
    }
}
