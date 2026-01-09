using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.GameTypes.AoC.Objects;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FAoCDataChunk
{
    public ulong Hash;
    public long Offset;
    public long Size;
    public int FileCount;
    public uint checksum;
}

[JsonConverter(typeof(FAoCDBCReaderConverter))]
public sealed class FAoCDBCReader : FAssetArchive
{
    private Dictionary<int, string> NameMap = [];
    public FAoCDataChunk[] Chunks = [];

    public FAoCDBCReader(byte[] data, TypeMappings? mappings, VersionContainer versions) : base(new FByteArchive("CacheDB", data, versions), new FakePackage("CacheDB", mappings))
    {
        var magic = Read<uint>();
        var version = magic >> 24;
        var hash = Read<ulong>();
        Position += 8;
        var chunkscount = Read<int>();
        Position += 16;
        var stringsOffset = Read<long>();
        var stringsChunkSize = Read<int>();
        var stringsCheckSum = Read<uint>();
        var chunksDataStart = Read<int>();
        Chunks = ReadArray<FAoCDataChunk>(chunkscount);

        Position = stringsOffset;
        while (Position < Length)
        {
            NameMap[(int)(Position- stringsOffset)] = ReadFString();
        }

        FRawHeader.FullRead = new([(0, -1)], ERawHeaderFlags.RawProperties | ERawHeaderFlags.SuperStructs);
    }

    public bool TryReadChunk(int index, out string category, out FAoCFile[] files)
    {
        category = "";
        files = [];
        if (index < 0 || index >= Chunks.Length) return false;

        Position = Chunks[index].Offset;
        if (!TypeMap.TryGetValue(Chunks[index].Hash, out var filetype))
        {
            Log.Warning($"Unknown AoC DBC Chunk Type Hash: {Chunks[index].Hash}");
            return false;
        }

        category = filetype;
        var type = filetype + filetype switch
        {
            "CertificationLevel" or "GameMusicOverride" or "RuntimeFoliageType"
                or "RuntimeFoliageSpawnDef" or "TestData" or "Toast" => "Record",
            "DestinySystem" or "EquipableComponent" or "FishingSizeData" or "InteractableStateTreeDef"
                or "InteractionStateDef" or "DialogueTable" or "DialogueNode" or "DialogueRoot"
                or "GlobalKillVolumeList" or "KillVolumeDef" or "WarPhaseRecordV2" or "WarRecordV2"
                or "NCSRemapCache" or "NarrativeQuestV2" or "NarrativeObjectiveV2" or "ParticipationNameRegistry" => "",
            _ when filetype.EndsWith("Record") => "",
            _ => "RecordBase",
        };
        try
        {
            files = ReadArray(Chunks[index].FileCount, () => new FAoCFile(this, type));
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to read CacheDB Chunk Type: {Type}", category);
            return false;
        }

        return true;
    }

    public IUStruct? ReadInstancedStruct()
    {
        var strucPath = ReadFString();
        var size = Read<int>();
        var saved = Position;
        if (string.IsNullOrEmpty(strucPath)) return null;
        var name = strucPath.SubstringAfterLast('.');
        try
        {
            return new FScriptStruct(this, name, null, ReadType.RAW).StructType;
        }
        catch
        {
            Log.Warning("Failed to read FInstancedStruct of type {0}, skipping it", strucPath);
        }
        finally
        {
            Position = saved + size;
        }

        return null;
    }

    public override FName ReadFName() => NameMap.TryGetValue(Read<int>(), out var name) ? new FName(name) : new FName("None");
    public override bool TestReadFName()
    {
        if (HasUnversionedProperties) return false;
        var savedPos = Position;
        if (Position + sizeof(int) >= Length) return false;
        var nameIndex = Read<int>();
        Position = savedPos;
        return nameIndex >= 0 && nameIndex < NameMap.Count;
    }

    private Dictionary<ulong, string> TypeMap = new()
    {
        {1841048597116038327L, "AoCAbility"},
        {6221802054776738159L, "CharacterAlignment"},
        {3672506661632452365L, "AbilityHit"},
        {5365104778484440215L, "StatTypeDef"},
        {3377240744094973029L, "Effect"},
        {1641972992788453644L, "AbilityAnimCancel"},
        {4582867823717639616L, "AbilityDisable"},
        {42595794548453256L, "AbilityFX"},
        {8717909394212734711L, "AbilityGrant"},
        {3999912890578843546L, "AbilityTargeting"},
        {1776538005552784248L, "ActionResponse"},
        {4671704023637093956L, "AgnosticMaterialInstanceTable"},
        {1417837979037806085L, "AIUseAbilityConditionRecord"},
        {8434552287477319980L, "AnimBranch"},
        {3329800360221666189L, "AoCLevelSequence"},
        {1556333153587415880L, "AOIFilterSettings"},
        {220589620517952189L, "AppearanceInfo"},
        {3942245912651291127L, "AssetSet"},
        {3775764267575595260L, "MapIconVisual"},
        {1184006482541657377L, "AttachedCharacterSpawnInfo"},
        {5959255739389286844L, "Attune"},
        {8067374428026703475L, "BaseItemValue"},
        {6865485065268376430L, "BiomeAreaRecord"},
        {4931577733934611804L, "AreaMapConstantsRecord"},
        {7851809832257164792L, "BoneEntitlements"},
        {8670079151239204580L, "BotBehavior"},
        {8869251564084934631L, "BotConfig"},
        {6247025323563028748L, "BotEquipmentSet"},
        {2755315174179199862L, "BotFollowPath"},
        {2125186787305827858L, "BotGenderSelection"},
        {5468782469582058610L, "BotPointSet"},
        {7719571796969067617L, "BotRaceSelection"},
        {1462727186074453664L, "BotSpawnArea"},
        {6644807124375120029L, "BotTeleportPoint"},
        {3277739448207944662L, "Building"},
        {4774231366602816874L, "BuildingLevelSpawn"},
        {4800667172105975952L, "BuildingNodeAssetSets"},
        {1643355224617067005L, "BuyOrderConstants"},
        {4973836439094637536L, "CannonizationState"},
        {1837697778193490828L, "CaravanConstants"},
        {2466654875685814814L, "CaravanConstruction"},
        {6758486465272513401L, "CaravanRecoveryBeacon"},
        {1094497263296717468L, "CertificationLevel"},
        {7464853447515897979L, "ChacterCustomizationColor"},
        {5122200902577457734L, "CharacterAlignmentContext"},
        {700453677354247146L, "CharacterAttachedParticleSystem"},
        {3107803182685678074L, "CharacterClasses"},
        {7078825747674056206L, "CharacterCustomizationEntitlements"},
        {5545277207925726636L, "CharacterCustomizationLightRig"},
        {4436787965034280954L, "CharacterMorph"},
        {1434483257069978249L, "CharacterMovement"},
        {5612931243805009072L, "CharacterProfessions"},
        {8713538519916081291L, "CharacterRelationship"},
        {7838555140437787604L, "CheaterHeatCategoryEntry"},
        {3847830603112910153L, "ChildNodeDesiredLevelRules"},
        {7177945509919195062L, "ChildNodeRules"},
        {6713806148578257698L, "CitizenshipDuesDef"},
        {1022498846691670211L, "CitizenshipGeneric"},
        {3333945169507649514L, "CitizenshipNode"},
        {8574414081702566619L, "CityNode"},
        {3263829181307747928L, "CityNodeConstants"},
        {1137740627926481120L, "CityNodeElectionDef"},
        {5733568505899896550L, "Commission"},
        {4001223916580902405L, "CommissionBoard"},
        {6417156823623140690L, "CommodityDef"},
        {9134735415063031070L, "CommodityRecipeDef"},
        {4487948344931545891L, "CommodityRecipeMaterialRequirementDef"},
        {7377443334195899876L, "CommodityRecipeMaterialTierDef"},
        {2089930537069581475L, "CommodityRecipeMaterialTierEntry"},
        {7962762001337101365L, "ConditionalBranch"},
        {7305486416673995120L, "ConditionalOverride"},
        {3702916240172843752L, "ContentCalendarRecord"},
        {4897295373799177325L, "ContextualModalSettings"},
        {3801099600946266068L, "ContributionThresholds"},
        {656194998503519894L, "CosmeticMeshSlot"},
        {5574969167440970101L, "CraftingCurrencyCost"},
        {9051245108308927306L, "CraftingRecipeDef"},
        {191163774835479387L, "CraftingRecipeList"},
        {7840571794229228245L, "CraftingRecipeSet"},
        {834245534357962010L, "CraftingStationDef"},
        {8441528838166389858L, "CraftingStationVisualDef"},
        {5519291182759101244L, "CraftingSubRecipeDef"},
        {8359201942781029513L, "Crate"},
        {2559080087696523252L, "CrowdControl"},
        {2297981969118768486L, "CrowdControlBreak"},
        {4345265865976112159L, "CrowdControlIgnoreBreak"},
        {223879564105432988L, "CurrencyDef"},
        {456318420933392641L, "CurrencyTierDef"},
        {735021282265672800L, "CustomizationPreset"},
        {7771346702747595626L, "DDTUIInfo"},
        {3277857146148615814L, "DDTUIInfoAssociation"},
        {199164474120233756L, "DecalAppearance"},
        {6277682454049782291L, "DecalGroup"},
        {1116361024258992267L, "DeconstructionConsumableDef"},
        {8700449142277571141L, "DefaultCustomFloatingTextSettings"},
        {2622888547920598380L, "Deployable"},
        {6843249658918504361L, "DestinationDef"},
        {2378867683473543797L, "DestinySystem"},
        {6434620470175178317L, "DialogAudioSettings"},
        {8244381015243442593L, "DialogueChoice"},
        {6071118650495728335L, "DialogueDef"},
        {4293100627603413582L, "DialogueGraphNode"},
        {241139857565909152L, "DialogueResponse"},
        {4196775139793011010L, "DistributionTable"},
        {2813355500012976422L, "EconomicRegionTable"},
        {1167259326759670518L, "EffectGroup"},
        {6475658782733535559L, "ElectionConstants"},
        {6312189548189534857L, "EmoteAnimationInfo"},
        {1130600751782640271L, "EmotesLookup"},
        {4451249104913741801L, "EnchantmentCharmDef"},
        {3711670483734750229L, "EnchantmentItemDef"},
        {4849668259771572475L, "EnchantmentLevelDef"},
        {2429056554932492469L, "EnchantmentScrollDef"},
        {7839250957252574587L, "EnchantmentVFXDef"},
        {8124469745983037765L, "EnvironmentAudioSettings"},
        {4439140063677818860L, "EquipableComponent"},
        {5437818069991473859L, "EquipmentAppearanceArchetype"},
        {2610763507815524465L, "EventDef"},
        {2748060231176463635L, "EventLocationDef"},
        {6642905922054034504L, "EventObjectiveDef"},
        {253951899146590253L, "EventPredicateDef"},
        {5077218730591431445L, "EventServiceParametersDef"},
        {7870704700803447979L, "EventStageDef"},
        {1520429840428565184L, "EventTrackerDef"},
        {3513492801269167025L, "EventTriggerDef"},
        {8187534410104834233L, "Expansion"},
        {7359648013239505706L, "ExperienceContext"},
        {1029425032685305274L, "ExperienceCurveSet"},
        {5438904159854115820L, "FastTravel"},
        {6140387333121499770L, "FilteringOverride"},
        {6036763354357278322L, "FirstTimeEvent"},
        {3734796074842165565L, "FishingSizeData"},
        {5630551750208934118L, "FootstepDecals"},
        {3645681529784039325L, "FragileEffectDef"},
        {1798480684826362847L, "FreeholdEstate"},
        {1842116118836762333L, "FTUEConstants"},
        {7938779629084741394L, "FVendorPriceModifier"},
        {7771424812268509830L, "GameMusic"},
        {2664612512739343003L, "GameMusicOverride"},
        {7682016176665870784L, "GameStatProfile"},
        {2392878644472371619L, "GatherableActorDef"},
        {239265429426660925L, "GatherableAttractorDef"},
        {9017096999030213245L, "GatheringDynamicDef"},
        {5936553769565586013L, "GatheringFoliageData"},
        {388431952879129939L, "GatheringFoliageState"},
        {4798995761981159336L, "GatheringInteractionData"},
        {6991977563119033198L, "GatheringResourceType"},
        {222715032364618132L, "GatheringToolData"},
        {162177776058976074L, "GatherItemFilter"},
        {855429863448366598L, "Gem"},
        {9139336732625199455L, "GroupConstants"},
        {6104153599545513250L, "GuildEffect"},
        {7248218746092253468L, "GuildGeneric"},
        {9124432112565034059L, "GuildGroup"},
        {9059494087008024700L, "GuildLevelingDef"},
        {7656681231958792789L, "HarvestManagerEntry"},
        {3461759442045483903L, "HeatMapDefinition"},
        {4269360798773811586L, "HitMod"},
        {1761906457282090271L, "House"},
        {5663197520006134589L, "HumanoidNPC"},
        {6524815930554660966L, "HumanoidOutfit"},
        {2909257786168991404L, "IntelligenceInfo"},
        {251533571123157423L, "InteractableActorDef"},
        {5088630916480188663L, "InteractableComponentDef"},
        {6134854545108131229L, "InteractableStateTreeDef"},
        {8980238299584116790L, "InteractionStateDef"},
        {3816514923245167133L, "InteriorAudioSettings"},
        {8565862518736932262L, "IntrepidAudio"},
        {2328801910919856698L, "ContributionThresholdRecord"},
        {6510012445023489570L, "IrisAppearance"},
        {2616523457551378613L, "Item"},
        {8853453557447911545L, "Item3DAppearance"},
        {4292131259496257600L, "ItemActivationArchetype"},
        {7961510888716503441L, "ItemAppearance"},
        {1519012493970093288L, "ItemAppearanceMatTint"},
        {3411376682345283973L, "ItemAppearanceSet"},
        {5344397996542147150L, "ItemAudio"},
        {4561317366989358001L, "ItemDistribution"},
        {2560911356549323713L, "ItemDistributionEntry"},
        {3996378489238650003L, "ItemDropRatio"},
        {6471621905245551332L, "ItemMaterial"},
        {5417504334752196420L, "ItemOcclusionArchetype"},
        {8766267161525892068L, "ItemPetData"},
        {6064238867039623168L, "ItemSocket"},
        {2209512634497779819L, "ItemSocketArchetype"},
        {4757144276206936031L, "ItemStatBlock"},
        {2835370578767603185L, "ItemTint"},
        {7640658097578260109L, "ItemVariation"},
        {4599937519242900987L, "Layout"},
        {287999671638600142L, "LevelDefinition"},
        {5949882024684912047L, "LevelingTypeDef"},
        {1070707959664480407L, "LevelZoneRecord"},
        {7793593157135267051L, "LingeringEffect"},
        {143598618651246137L, "LoadingScreenData"},
        {8786632199526485205L, "Locale"},
        {8670016893400606125L, "LocaleContainer"},
        {3684257988581240814L, "LocaleOrigin"},
        {9046526576657284069L, "LocaleRequestDef"},
        {2175386539714333180L, "LootPileVFXDef"},
        {2264863414808828549L, "MapIconInfo"},
        {8811189518662194526L, "MapIconMappings"},
        {7716696395773702375L, "MapIndicator"},
        {6689747473418639285L, "MapLegendEntry"},
        {5551905168185487477L, "MapRegistration"},
        {1661556087166471525L, "MapTileSet"},
        {4945115678128942392L, "MasterAttune"},
        {389970842752241559L, "MasterDiscovery"},
        {4840391045360670673L, "MergeSectionMaterial"},
        {459033338675683531L, "ModifyCooldown"},
        {54465715294628190L, "ModifyEffect"},
        {4242536852138555411L, "Music"},
        {6649758200974839062L, "NamedLocationRecord"},
        {2547654495776762682L, "NameplateInfo"},
        {5787205031762440444L, "NarrativeCriteria"},
        {523240889758054542L, "DialogueTable"},
        {5024462864295026017L, "DialogueNode"},
        {1530085698787182412L, "DialogueRoot"},
        {1120337494274161717L, "NarrativeObjective"},
        {4579639606164595207L, "NarrativeRequirements"},
        {6809745616796720836L, "NodeAssetDefinition"},
        {2174812910488237127L, "NodeAssetDefinitionStaticInstanceDataConstants"},
        {8886906632445377512L, "NodeAssetSet"},
        {100629647063453269L, "NodeDefinition"},
        {7511613499462852052L, "NodeEffect"},
        {3581726814900249451L, "NodeGlobalTaxTable"},
        {3503680057278988807L, "NodeInventoryContents"},
        {2738744054449994660L, "NodeLevelDetails"},
        {5490958306072702754L, "NodeModification"},
        {2699521489519275590L, "NodeNotification"},
        {2758714873714911180L, "NodeReward"},
        {3560285981519329392L, "NodeSize"},
        {6940560660941959840L, "NodeToPlayerReputationTier"},
        {8766912787626414116L, "NpcAIBehaviorNode"},
        {3148188350543168811L, "NpcAIBlackboard"},
        {5100415408508301366L, "NpcAIConsideration"},
        {5299156953604555893L, "NpcAIScoreEvaluator"},
        {696524202718735427L, "NpcAITransitionExpression"},
        {3759548309986862969L, "NPCAppearance"},
        {7603391734862419090L, "NPCDefinition"},
        {6411310023816429525L, "NPCTemplate"},
        {4794207675813008353L, "OptionsMenuPage"},
        {5029294351135732735L, "PathingRoute"},
        {2598120377796491089L, "PetCommand"},
        {8029888758683501854L, "PlayerCharacterAudio"},
        {2949570675404937618L, "PlayerInteractionDef"},
        {389292245082160889L, "PlayerSpawnFallbackLocation"},
        {2197448624304014653L, "PlayerStarts"},
        {3677751352029065476L, "POI"},
        {9027302322125214578L, "PoiDiscovery"},
        {3918448705985772659L, "PopulationArea"},
        {9191965118498618338L, "PopulationAsset"},
        {6814160152926513068L, "PopulationInstance"},
        {3052335037205854904L, "PopulationInstanceUIAssociation"},
        {3019708863118852195L, "PopulationInstanceUIInfo"},
        {3392875243664795609L, "PopulationInteractVolume"},
        {2959717270924512690L, "PopulationSet"},
        {3249602469899113919L, "PortraitRenderProfile"},
        {1630047317340324195L, "PredicateDef"},
        {7180648142926436099L, "ProgressDisruption"},
        {979581256175327151L, "Projectile"},
        {7341791279741245342L, "PropertyTaxDuesDef"},
        {2763533897913176681L, "QualityCurve"},
        {3573031127198855575L, "QuantityScalarDef"},
        {6170063386889671940L, "RaceGenderAppearance"},
        {2578134533504376814L, "RaceGenderAppearanceMap"},
        {5230349797545258222L, "RaceGenderCustomizationDef"},
        {5850052713160755418L, "RandomGatherableSpawnersDef"},
        {6091248426050982904L, "Reaction"},
        {975350056047082510L, "Redirect"},
        {8671545008291730102L, "RegionalTask"},
        {6503011985655461955L, "RegionalTaskCollection"},
        {2973815626812803864L, "Relic"},
        {536498122450699087L, "Requirement"},
        {5464295586424667318L, "Resistance"},
        {6026413454960341687L, "ResourceBagDef"},
        {7326889879645706054L, "ResourceCost"},
        {8652894870474375187L, "Respawn"},
        {8656627123103578483L, "Resurrect"},
        {2615607320058196115L, "RewardTable"},
        {134026899431943455L, "RoadAppearance"},
        {6406011422610129867L, "RoadBaseAppearance"},
        {8211055807357762983L, "RoadPreset"},
        {7800146198219293089L, "Rtpcs"},
        {3933045754662041395L, "RuntimeFoliageType"},
        {5510815885634747495L, "RuntimeFoliageSpawnDef"},
        {5265849268557898785L, "ScleraAppearance"},
        {4292213401668875298L, "ServiceBuildingPlot"},
        {1330963460908541034L, "SetBonus"},
        {77014339219681019L, "SiegeAudio"},
        {2451706128419512830L, "GlobalKillVolumeList"},
        {7887485711601353236L, "KillVolumeDef"},
        {4146438674247114715L, "Skill"},
        {7337271797676719493L, "SkillPointType"},
        {4847957796014348780L, "SkillRank"},
        {7584189049858318272L, "SkillRequirement"},
        {297887639323616934L, "Skilltree"},
        {7379564169480974333L, "SkillTreeCell"},
        {7532494710696707005L, "SkillTreeCellTable"},
        {1084214640716601260L, "SkillTreeNode"},
        {5531284426918814767L, "SkillTreeSlot"},
        {3300871662499502057L, "SkinColorSet"},
        {2717988358395411729L, "SkinMesh"},
        {1129970719172867241L, "SkinMeshSet"},
        {3934493312775601284L, "SkinPBR"},
        {2851954489771253619L, "SocialConstants"},
        {295599187369307348L, "SpatialAnalyzerSettings"},
        {3523432441823791405L, "SpawnLingeringZone"},
        {767954206867959579L, "SpawnLocale"},
        {3028394854558971400L, "SpawnLocation"},
        {1332327274597769533L, "SpawnNPC"},
        {1786103727538726971L, "SpecializedWarehouseTabDetails"},
        {8702546916752658793L, "SpecialLocationRecord"},
        {2502640340238122461L, "SpecificItem"},
        {6131977029187292797L, "SportFishingSpawnTable"},
        {1883488081666746542L, "StartingBackground"},
        {8457388432688083496L, "StartingEquipment"},
        {1395318756489997343L, "StartingStats"},
        {9119294998841828153L, "StartingStatsClass"},
        {5160932447857008854L, "StartingStatsRace"},
        {1611925239468316012L, "StartingZone"},
        {8513040259781315802L, "StatBlock"},
        {2234376613565266810L, "StatCaptureSpec"},
        {7037248826340794574L, "StatCurve"},
        {1393768151127144893L, "StateGroup"},
        {7180619218012719533L, "StatEquationType"},
        {6249280735402452246L, "States"},
        {4547065559311335368L, "StatFormulaType"},
        {3757329144584700494L, "StaticDesignTable"},
        {5488972381814748242L, "StaticLocaleOrigin"},
        {2937500229480787749L, "StatInitializerList"},
        {7556084618804097379L, "StatLevelGains"},
        {7571796268333552826L, "StatLevelGainsClass"},
        {2017005298167588428L, "StatLevelGainsRace"},
        {676208982154769454L, "StatMod"},
        {5743482634043199023L, "StatusEffectVFXMapping"},
        {9043907334218597952L, "StatusVFX"},
        {5538224538808749028L, "StoryArc"},
        {4610357610403302003L, "StoryArcPhase"},
        {3132157286228547964L, "StoryArcPhaseSpawnLocation"},
        {3778852817743595931L, "StoryArcQuest"},
        {5775213298276601945L, "StoryArcTemplate"},
        {4424211922891023187L, "SurveyPylonDef"},
        {8144210009820671919L, "SurveyPylonVisualDef"},
        {4783563225603123221L, "Switches"},
        {5061605918689506639L, "SwitchGroup"},
        {5249343647376095068L, "TargetingInfo"},
        {2556663074337263590L, "Task"},
        {8728435148670934154L, "TaxDetails"},
        {362561827980444562L, "Teleport"},
        {8718996807662557265L, "TeleportationDestinationData"},
        {3899392888792329524L, "TemperingCost"},
        {4753247786199585904L, "TemperingKitDef"},
        {6137747221710071900L, "Term"},
        {3426165919709581445L, "TestData"},
        {5843841379919361115L, "TestScenario"},
        {4338515574695111038L, "TimedDespawn"},
        {1268519013215452177L, "TimesOfDay"},
        {3201732056065770581L, "Toast"},
        {1656814815510384866L, "TreasureHunt"},
        {8120648054488450269L, "TreasurePlacement"},
        {7690244654297104194L, "Triggers"},
        {3167159759459649097L, "UIRarityProps"},
        {3908836359344192144L, "UITutorialData"},
        {3172969313194574051L, "VassalshipDef"},
        {1132270495744744113L, "VehicleQuestPoolRecord"},
        {7583966727761861962L, "VehicleTemplate"},
        {1717174962815593486L, "VendorDef"},
        {7385378364265332965L, "VendorInventoryDef"},
        {3286260905745857630L, "VendorOffer"},
        {9111089874293943428L, "VendorSellFilter"},
        {7926774586810711355L, "VFXInfo"},
        {2273639125439972134L, "VictoryPointTable"},
        {865739595354152117L, "Voice"},
        {2810495278941476814L, "VoiceAudio"},
        {7684368977969823848L, "VoiceAudioText"},
        {6531184464978151283L, "War"},
        {4866978429317970547L, "WarConstants"},
        {7845233755630994259L, "WarehouseStorageDef"},
        {493388405103239456L, "WarehouseStorageItemChunk"},
        {6541401997579662949L, "WarehouseStorageItemChunksDef"},
        {8728563133852991942L, "WarehouseStorageMaterialTab"},
        {8003852666215472197L, "WarPhase"},
        {1826293229611675476L, "WarReward"},
        {4248916874496595183L, "WarPhaseRecordV2"},
        {6904455205957373543L, "WarRecordV2"},
        {7711002412429888222L, "WeaponSkillLines"},
        {8746844264917987310L, "WeaponSmithingBuilding"},
        {1789507293266432139L, "WeaponSmithingRecipeDef"},
        {786698471310825737L, "Windows"},
        {8200963678842873603L, "WindowWidgets"},
        {4570109678092719558L, "WorldSpawn"},
        {2027519232306129775L, "WorldStormSystems"},
        {3967249427236365357L, "ZOI"},
        {8995484892549588227L, "NCSRemapCache"},
        {6732126019658962568L, "NarrativeQuestV2"},
        {2174867120698124769L, "NarrativeObjectiveV2"},
        {4065361913145052288L, "ContentSocketRecord"},
        {994452707346827846L, "ParticipationNameRegistry"}

    };
}

public class FAoCDBCReaderConverter : JsonConverter<FAoCDBCReader>
{
    public override FAoCDBCReader? ReadJson(JsonReader reader, Type objectType, FAoCDBCReader? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FAoCDBCReader? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Chunks));
        serializer.Serialize(writer, value.Chunks);

        writer.WriteEndObject();
    }
}
