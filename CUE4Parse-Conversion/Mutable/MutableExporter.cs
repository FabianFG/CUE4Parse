using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Image;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using Serilog;
using SkiaSharp;

namespace CUE4Parse_Conversion.Mutable;

public class MutableExporter : ExporterBase
{
    // <SkeletonName, (MeshName, Mesh)>
    public readonly Dictionary<string, List<Tuple<string, Mesh>>> Objects;
    public readonly List<SKBitmap> Images;
    
    public MutableExporter(UCustomizableObject originalCustomizableObject, ExporterOptions options, AbstractVfsFileProvider provider) : base(originalCustomizableObject, options)
    {
        Objects = [];
        Images = [];

        // <skeletonIndex, <MaterialSlot, Meshes>>
        // Sort into Meshes folder, then at the end pass in lists of meshes to Converter to handle LODs?
        Dictionary<uint, Dictionary<string, List<FMesh>>> meshes = [];
        
        var evaluator = new MutableEvaluator(provider, originalCustomizableObject);
        evaluator.LoadModelStreamable();
        
        var coPrivate = originalCustomizableObject.Get<FPackageIndex>("Private").Load();
        var modelResources = coPrivate.Get<FStructFallback>("ModelResources");
        var surfaceNameMap = GetSurfaceNameMap(modelResources);
        var boneNameMap = modelResources.Get<UScriptMap>("BoneNamesMap");
        var skeletons = modelResources.Get<FSoftObjectPath[]>("Skeletons");

        foreach (var rom in originalCustomizableObject.Model.Program.Roms)
        {
            switch (rom.ResourceType)
            {
                case DataType.DT_IMAGE:
                    var image = evaluator.LoadImageResource((int)rom.ResourceIndex);
                    if (image is { IsBroken: false })
                        ExportMutableImage(image);
                    break;
                case DataType.DT_MESH:
                    var mesh = evaluator.LoadResource((int) rom.ResourceIndex);
                    if (mesh is { IsBroken: false })
                        StoreMutableMesh(mesh, meshes, surfaceNameMap);
                    break;
                default:
                    Log.Information("Unknown resource type: {0} for index: {1}", rom.ResourceType, rom.ResourceIndex);
                    break;
            }
        }

        if (meshes.Count > 0)
            ExportMutableMeshes(meshes, skeletons);
    }
    
    private Dictionary<uint, string> GetSurfaceNameMap(FStructFallback modelResources)
    {
        Dictionary<uint, string> surfaceNameMap = [];
        
        var meshMetadata = modelResources.Get<UScriptMap>("MeshMetadata");
        var surfaceMetadata = modelResources.Get<UScriptMap>("SurfaceMetadata");

        foreach (var meshEntry in meshMetadata.Properties)
        {
            var surfaceID = meshEntry.Value.GetValue<FStructFallback>().Get<uint>("SurfaceMetadataId");
            var surfaceEntry = surfaceMetadata.Properties.First(key => key.Key.GetValue<uint>() == Convert.ToUInt32(surfaceID));
            var materialSlotName = surfaceEntry.Value.GetValue<FStructFallback>().Get<FName>("MaterialSlotName").PlainText;
            surfaceNameMap.Add(meshEntry.Key.GetValue<uint>(), materialSlotName);
        }

        return surfaceNameMap;
    }
    
    private void StoreMutableMesh(FMesh mesh, Dictionary<uint, Dictionary<string, List<FMesh>>> meshes, Dictionary<uint, string> surfaceNameMap)
    {
        var skeletonIndex = mesh.SkeletonIDs.Last();
        var materialSlotName = surfaceNameMap[mesh.Surfaces[0].SubMeshes[0].ExternalId].Replace("_LOD", "");
        
        // TODO: Remove temp limit
        if (materialSlotName.Contains("Wheel", StringComparison.OrdinalIgnoreCase)) return;

        if (!meshes.ContainsKey(skeletonIndex))
            meshes[skeletonIndex] = [];

        if (!meshes[skeletonIndex].ContainsKey(materialSlotName))
            meshes[skeletonIndex][materialSlotName] = [];

        meshes[skeletonIndex][materialSlotName].Add(mesh);
    }

    private void ExportMutableMeshes(Dictionary<uint, Dictionary<string, List<FMesh>>> meshes,
        FSoftObjectPath[] skeletons)
    {
        foreach (var skeletonGroup in meshes)
        {
            var skeleton = skeletons[skeletonGroup.Key].Load<USkeleton>();
            if (skeleton.Name.Contains("SK_Wheel", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var materialGroup in skeletonGroup.Value)
            {
                var sortedList = materialGroup.Value.OrderByDescending(mesh => mesh.VertexBuffers.ElementCount)
                    .ToList();
                ExportMutableMesh(sortedList, materialGroup.Key, skeleton);
            }
        }
    }
    
    private void ExportMutableMesh(List<FMesh> meshes, string materialSlotName, USkeleton skeleton)
    {
        var mesh = meshes[0];
        meshes.RemoveAt(0);
        
        if (!mesh.TryConvert(materialSlotName, skeleton, out var convertedMesh, meshes) || convertedMesh.LODs.Count == 0)
        {
            Log.Logger.Warning($"Mesh '{ExportName}' has no LODs");
            return;
        }
        
        var meshName = $"{skeleton.Name}_{materialSlotName}";
        var exportPath = $"{PackagePath}/{skeleton.Name}/{meshName}";
        
        var totalSockets = new List<FPackageIndex>();
        if (Options.SocketFormat != ESocketFormat.None)
        {
            totalSockets.AddRange(skeleton.Sockets);
        }

        if (Options.MeshFormat == EMeshFormat.UEFormat)
        {
            using var ueModelArchive = new FArchiveWriter();
            new UEModel(meshName, convertedMesh, null, totalSockets.ToArray(), null, Options).Save(ueModelArchive);
            var outputMesh = new Mesh($"{meshName}.uemodel", ueModelArchive.GetBuffer(), convertedMesh.LODs[0].GetMaterials(Options));
            
            if (!Objects.ContainsKey(skeleton.Name))
                Objects.Add(skeleton.Name, []);
                
            Objects[skeleton.Name].Add(new Tuple<string, Mesh>(exportPath, outputMesh));
            return;
        }
        // TODO: other types
    }

    private void ExportMutableImage(FImage image)
    {
        var bitmap = image.Decode();
        if (bitmap != null) Images.Add(bitmap);
    }

    public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
    {
        savedFilePath = "TempFilePath";
        label = "Mutable";
        return false;
    }

    public override bool TryWriteToZip(out byte[] zipFile)
    {
        throw new System.NotImplementedException();
    }

    public override void AppendToZip()
    {
        throw new System.NotImplementedException();
    }

    private static Dictionary<string, List<string>> CarIDs = new()
    {
        ["Admiral"] = Admiral,
        ["Backfire"] = Backfire,
        ["Behemoth"] = Behemoth,
        ["Beskar"] = Beskar,
        ["BMW_1_T1"] = BMW_1_T1,
        ["BMW_1_T2"] = BMW_1_T2,
        ["BMW_M240I"] = BMW_M240I,
        ["Centio"] = Centio,
        ["Cybertruck"] = Cybertruck,
        ["Cyclone"] = Cyclone,
        ["Diesel"] = Diesel,
        ["Diestro"] = Diestro,
        ["Dominus"] = Dominus,
        ["Endo"] = Endo,
        ["Fairlady_T1"] = Fairlady_T1,
        ["Fairlady_T2"] = Fairlady_T2,
        ["Fuse"] = Fuse,
        ["Huracan"] = Huracan,
        ["Imperator"] = Imperator,
        ["Insidio"] = Insidio,
        ["Jager_619"] = Jager_619,
        ["Lockjaw"] = Lockjaw,
        ["Mako"] = Mako,
        ["Masamune"] = Masamune,
        ["Mclaren_765LT"] = Mclaren_765LT,
        ["Mclaren_Senna"] = Mclaren_Senna,
        ["Nissan_Skyline_FF"] = Nissan_Skyline_FF,
        ["Nissan_Z_Performance"] = Nissan_Z_Performance,
        ["Nimbus_Unreleased"] = Nimbus_Unreleased,
        ["Octane"] = Octane,
        ["Octane_ZSR"] = Octane_ZSR,
        ["Peregrine"] = Peregrine,
        ["Redline"] = Redline,
        ["Samarai"] = Samarai,
        ["Scorpion"] = Scorpion,
        ["SUV"] = SUV,
        ["Werewolf"] = Werewolf
    };

    private static List<string> Admiral = ["1558_710194167_0", "1559_3642310659_0", "1560_1830572063_0", "1561_1845266641_0", "1562_1423472731_0","1563_368636361_0", "1564_2373134345_0"];
    private static List<string> Backfire = ["1678_1787002871_0", "1679_2387926023_0", "1680_1781829455_0", "681_1149073306_0"];
    private static List<string> Behemoth = ["1462_3630556097_0", "1463_2753242837_0", "1464_782765615_0", "1465_125366478_0", "1466_2844144182_0", "1467_942942748_0", "1468_2290618438_0", "1469_2983927905_0", "1470_687617426_0"];
    private static List<string> Beskar = ["485_3271870737_0", "1486_1723849897_0", "1487_925573001_0", "1488_1064927260_0", "1489_2359056864_0", "1490_1252515115_0", "1491_2103189347_0", "1492_320878443_0", "1493_2764934284_0", "1494_2494278084_0", "1495_4134818193_0", "1496_2482856642_0", "1497_3148299665_0", "1498_2246458751_0", "1499_2692373510_0"];
    private static List<string> BMW_1_T1 = ["1671_2109876619_0", "1672_3667623677_0", "1673_94554670_0", "1674_3138223058_0", "1675_573961813_0", "1676_296344979_0", "1677_3819232068_0"];
    private static List<string> BMW_1_T2 = ["1664_1066941655_0", "1665_465043669_0", "1666_1578001709_0", "1667_2832793917_0", "1668_2276639494_0", "1669_571992025_0", "1670_102400658_0", "1445_618036312_0"];
    private static List<string> BMW_M240I = ["1505_2012607782_0", "1506_994857619_0", "1507_2803808103_0", "1508_1402102987_0", "1509_2707106307_0", "1510_3496905301_0", "1511_1681089234_0"];
    private static List<string> Centio = ["1546_3283731758_0", "1547_2003437393_0", "1548_1397189575_0", "1549_3690856715_0", "1550_3140599079_0", "1551_877772230_0"];
    private static List<string> Cybertruck = ["1633_2959412617_0", "1634_3830585228_0", "1635_383565819_0", "1636_2589027645_0", "1637_2429824401_0", "1638_1738269476_0", "1639_869971755_0", "1640_2562703244_0", "1641_2170079304_0"];
    private static List<string> Cyclone = ["1471_2675452585_0", "1472_2314610332_0", "1473_1745452188_0", "474_2174123293_0", "1475_2279515266_0", "1476_2514454289_0"];
    private static List<string> Diesel = ["1531_2465048011_0", "1532_3719705921_0", "1533_2612360885_0", "1534_738335133_0", "1535_3343754901_0", "1536_1314770947_0", "1537_1588460992_0"];
    private static List<string> Diestro = ["1647_1878522973_0", "1648_1546491482_0", "1649_1074350024_0", "1650_3244847428_0", "1651_1861440905_0", "1652_3922400275_0"];
    private static List<string> Dominus = ["1588_3455361983_0", "1589_4081764942_0", "1590_1037048673_0", "1591_1292230258_0", "1592_1648009352_0"];
    private static List<string> Endo = ["1446_3167418863_0", "1642_1525552582_0", "1643_3340870114_0", "1644_2295064346_0", "1645_1031591106_0", "1646_3270782621_0"];
    private static List<string> Fairlady_T1 = ["1690_818121629_0", "1691_934201321_0", "1692_4063004198_0", "1693_2107209296_0", "694_2096820401_0", "1695_2295863327_0", "1696_1775507275_0", "1697_2343050884_0"];
    private static List<string> Fairlady_T2 = ["1682_3027870862_0", "1683_3924054210_0", "1684_386296035_0", "1685_671923452_0", "1686_278984554_0", "1687_2682005646_0", "1688_941914938_0", "1689_2153737533_0"];
    private static List<string> Fuse = ["1455_2955765166_0", "1456_1590902942_0", "1457_2794700754_0", "1458_58665837_0", "1459_3375270377_0", "1460_238523151_0", "1461_3345779918_0"];
    private static List<string> Huracan = ["1571_1240299615_0", "1572_1387255483_0", "1573_791263195_0", "1574_3201634848_0", "1575_3074742043_0", "1576_1555804263_0", "1447_507311478_0"];
    private static List<string> Imperator = ["1593_1507573710_0", "1594_310018425_0", "1595_3793860517_0", "1596_3932196863_0", "1597_2759597982_0", "1598_3993193301_0"];
    private static List<string> Insidio = ["1552_3415405039_0", "1553_623836898_0", "1554_1120997889_0", "1555_3074645455_0", "1556_4073905138_0", "1557_932317590_0"];
    private static List<string> Jager_619 = ["1448_2463698982_0", "1500_104520246_0", "1501_188291109_0", "1502_3063643091_0", "1503_4251832374_0", "1504_355446573_0"];
    private static List<string> Lockjaw = ["1477_310485348_0", "1478_4180024496_0", "1479_1184591112_0", "1480_4102728923_0", "1481_3679972738_0", "1482_1380927679_0", "1483_772336558_0", "1484_799979109_0"];
    private static List<string> Mako = ["1612_4208655611_0", "1613_340972409_0", "1614_1649945067_0", "1615_3087380648_0", "1616_68282623_0", "1617_2806657327_0", "1618_557085971_0"];
    private static List<string> Masamune = ["1583_2288905245_0", "1584_1648923930_0", "1585_114734268_0", "1586_2105594898_0", "1587_494129144_0"];
    private static List<string> Mclaren_765LT = ["1538_701450162_0", "1539_3929183896_0", "1540_2347014140_0", "1541_824182403_0", "1542_919149985_0", "1543_4009431167_0", "1544_1051932706_0", "1545_2531865148_0"];
    private static List<string> Mclaren_Senna = ["1653_3322677916_0", "1654_3146268514_0", "1655_2518040748_0", "1656_3771924965_0", "1657_411390489_0", "1658_342685256_0", "1659_1657291127_0", "1660_2649537029_0", "1661_1488232233_0", "1662_1509191235_0", "1663_1520252295_0"];
    private static List<string> Nissan_Skyline_FF = ["1599_2548997210_0", "1600_850244164_0", "1601_80397426_0", "1602_2610553348_0", "1603_162111110_0", "1604_2288305146_0", "1605_3634549555_0"];
    private static List<string> Nissan_Z_Performance = ["1517_1155842388_0", "1518_703084101_0", "1519_1672458739_0", "1520_2857957089_0", "1521_2804641852_0", "1522_1629832332_0", "1523_2593431110_0"];
    private static List<string> Nimbus_Unreleased = ["1524_2618704361_0", "1525_2667339790_0", "1526_2096835148_0", "1527_512060018_0", "1528_76708067_0", "1529_2581491349_0", "1530_1492923444_0"];
    private static List<string> Octane = ["1565_2452196937_0", "566_3958114968_0", "1567_1851363563_0", "1568_2224238240_0", "1569_1349059107_0", "1570_3342552139_0"];
    private static List<string> Octane_ZSR = ["1577_3339319147_0", "1578_1311862304_0", "1579_3988116017_0", "1580_4028897214_0", "1581_3891100327_0", "1582_3170268693_0"];
    private static List<string> Peregrine = ["1512_3051548937_0", "1513_109339436_0", "1514_3859284139_0", "1515_3011323297_0", "1516_3544096397_0"];
    private static List<string> Redline = ["1619_684130904_0", "1620_2351849405_0", "1621_1476160528_0", "1622_2406982510_0", "1623_1331081867_0", "1624_2097598060_0", "1625_3559401295_0", "1626_1159692709_0"];
    private static List<string> Samarai = ["1449_2214022129_0", "1450_3952631104_0", "1451_3103146728_0", "1452_3391712579_0", "1453_2724812585_0", "1454_3761368244_0"];
    private static List<string> Scorpion = ["1627_2771042982_0", "1628_3361507218_0", "1629_2924519778_0", "1630_1227413781_0", "1631_1533966541_0", "1632_3664418286_0"];
    private static List<string> SUV = ["1698_2429188867_0", "1699_737260532_0", "1700_3720109025_0", "1701_1815274495_0"];
    private static List<string> Werewolf = ["1606_3746079348_0", "1607_1379896820_0", "1608_3841986671_0", "1609_1792517_0", "1610_2924967296_0", "1611_546759802_0"];
}
