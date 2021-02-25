using System;
using System.Collections.Generic;
using CUE4Parse.UE4.AssetRegistry.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public class FAssetData
    {
        public readonly FName ObjectPath;
        public readonly FName PackagePath;
        public readonly FName AssetClass;
        public readonly FName PackageName;
        public readonly FName AssetName;
        public IDictionary<FName, string> TagsAndValues;
        public FAssetBundleData TaggedAssetBundles;
        public readonly int[] ChunkIDs;
        public readonly uint PackageFlag;

        public FAssetData(FAssetRegistryArchive Ar)
        {
            ObjectPath = Ar.ReadFName();
            PackagePath = Ar.ReadFName();
            AssetClass = Ar.ReadFName();
            PackageName = Ar.ReadFName();
            AssetName = Ar.ReadFName();

            Ar.SerializeTagsAndBundles(this);
            
            ChunkIDs = Ar.ReadArray<int>();
            PackageFlag = Ar.Read<uint>();
        }
    }
}