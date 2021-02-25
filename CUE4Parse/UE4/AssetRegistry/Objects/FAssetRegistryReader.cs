using System;
using System.Collections.Generic;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FAssetRegistryReader : FAssetRegistryArchive
    {
        private const uint _ASSET_REGISTRY_NUMBERED_NAME_BIT = 0x80000000u; // int32 max
        public readonly FStore Tags;
        
        public FAssetRegistryReader(FArchive Ar) : base(Ar)
        {
            var size = Ar.Read<int>();
            Ar.Position += 4 + 8;
            Ar.Position += 8L * size;
            NameMap = FNameEntrySerialized.LoadRegistryNames(Ar, size); // special stuff that gets all headers first
            Tags = new FStore(this);
        }
        
        public override FName ReadFName()
        {
            var nameIndex = baseArchive.Read<int>();
            var number = 0;
            if ((nameIndex & _ASSET_REGISTRY_NUMBERED_NAME_BIT) > 0u)
            {
                nameIndex -= Convert.ToInt32(_ASSET_REGISTRY_NUMBERED_NAME_BIT);
                number = baseArchive.Read<int>();
            }
            
#if !NO_FNAME_VALIDATION
            if (nameIndex < 0 || nameIndex >= NameMap.Length)
            {
                throw new ParserException(baseArchive, $"FName could not be read, requested index {nameIndex}, name map size {NameMap.Length}");
            }
#endif
            return new FName(NameMap[nameIndex], nameIndex, number);
        }

        public override void SerializeTagsAndBundles(FAssetData assetData)
        {
            var size = baseArchive.Read<ulong>();
            var ret = new Dictionary<FName, string>();
            var mapHandle = FPartialMapHandle.MakeFullHandle(Tags, size);
            foreach (var m in mapHandle.GetEnumerable())
            {
                ret[m.Key] = FValueHandle.GetString(Tags, m.Value);
            }
            
            assetData.TagsAndValues = ret;
            assetData.TaggedAssetBundles = new FAssetBundleData(this);
        }

        public override object Clone() => new FAssetRegistryReader((FArchive) baseArchive.Clone());
    }
}