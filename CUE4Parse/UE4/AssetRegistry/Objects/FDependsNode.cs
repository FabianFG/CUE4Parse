using System.Collections;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.AssetRegistry.Objects;

[JsonConverter(typeof(FDependsNodeConverter))]
public class FDependsNode
{
    private static readonly BitArray EmptyBitArray = new BitArray(0);

    private const int PackageFlagWidth = 3;
    private const int PackageFlagSetWidth = 5; // FPropertyCombinationPack3::StorageBitCount
    private const int ManageFlagWidth = 1;
    private int ManageFlagSetWidth(FArchive Ar) => Ar.Game >= EGame.GAME_UE5_7 ? 3 : 1;

    public FAssetIdentifier? Identifier;
    public int[] PackageDependencies;
    public int[] NameDependencies;
    public int[] ManageDependencies;
    public int[] Referencers;
    public BitArray PackageFlags;
    public BitArray ManageFlags;

    internal readonly int _index;

    public FDependsNode(int index)
    {
        _index = index;
    }

    public void SerializeLoad(FAssetRegistryArchive Ar)
    {
        Identifier = new FAssetIdentifier(Ar);
        PackageDependencies = Ar.ReadArray<int>();
        var numFlagWords = (PackageFlagSetWidth * PackageDependencies.Length).DivideAndRoundUp(32);
        PackageFlags = numFlagWords != 0 ? new BitArray(Ar.ReadArray<int>(numFlagWords)) : EmptyBitArray;
        NameDependencies = Ar.ReadArray<int>();
        ManageDependencies = Ar.ReadArray<int>();
        numFlagWords = (ManageFlagSetWidth(Ar) * ManageDependencies.Length).DivideAndRoundUp(32);
        ManageFlags = numFlagWords != 0 ? new BitArray(Ar.ReadArray<int>(numFlagWords)) : EmptyBitArray;
        Referencers = Ar.ReadArray<int>();
    }

    public void SerializeLoad_BeforeFlags(FAssetRegistryArchive Ar)
    {
        Identifier = new FAssetIdentifier(Ar);

        var numHard = Ar.Read<int>();
        var numSoft = Ar.Read<int>();
        var numName = Ar.Read<int>();
        var numSoftManage = Ar.Read<int>();
        var numHardManage = Ar.Header.Version >= FAssetRegistryVersionType.AddedHardManage ? Ar.Read<int>() : 0;
        var numReferencers = Ar.Read<int>();

        PackageDependencies = Ar.ReadArray<int>(numHard + numSoft);
        NameDependencies = Ar.ReadArray<int>(numName);
        ManageDependencies = Ar.ReadArray<int>(numSoftManage + numHardManage);
        Referencers = Ar.ReadArray<int>(numReferencers);
        PackageFlags = EmptyBitArray;
        ManageFlags = EmptyBitArray;
    }
}
