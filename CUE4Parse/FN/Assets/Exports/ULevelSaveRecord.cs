using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Ionic.Crc;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.FN.Assets.Exports
{
    // GUID: new(0xA76CBC6B, 0x51634CEE, 0x887E17DE, 0x463D4395)
    public enum ELevelSaveRecordVersion : short
    {
        CloudSaveInfoAdded,
        TimestampConversion,
        SoftActorClassReferences,
        SoftActorComponentClassReferences,
        DuplicateNewActorRecordsRemoved,
        StartOfResaveWhenNotLatestVersion,
        LowerCloseThresholdForDuplicates,
        DeprecatedDeleteAndNewActorRecords,
        DependenciesFromSerializedWorld,
        RemovingSerializedDependencies,
        AddingVolumeInfoRecordsMap,
        AddingVolumeGridDependency,
        AddingScale,
        AddingDataHash,
        AddedIslandTemplateId,
        AddedLevelStreamedDeleteRecord,
        UsingSaveActorGUID,
        UsingActorFNameForEditorSpawning,
        AddedPlayerPersistenceUserWipeNumber,
        Unused,
        AddedVkPalette,
        SwitchingToCoreSerialization,
        AddedNavmeshRequired,

        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public class FLevelSaveRecordArchive : FAssetArchive
    {
        protected readonly FArchive InnerArchive;
        public readonly ELevelSaveRecordVersion Version;

        public FLevelSaveRecordArchive(FAssetArchive Ar, ELevelSaveRecordVersion version) : base(Ar, Ar.Owner, Ar.AbsoluteOffset)
        {
            InnerArchive = Ar;
            Version = version;
        }

        public FLevelSaveRecordArchive(FArchive Ar, ELevelSaveRecordVersion version) : base(Ar, null)
        {
            InnerArchive = Ar;
            Version = version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count) => InnerArchive.Read(buffer, offset, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Seek(long offset, SeekOrigin origin) => InnerArchive.Seek(offset, origin);

        public override bool CanSeek => InnerArchive.CanSeek;
        public override long Length => InnerArchive.Length;
        public override long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InnerArchive.Position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => InnerArchive.Position = value;
        }

        public override string Name => InnerArchive.Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T Read<T>() => InnerArchive.Read<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] ReadBytes(int length) => InnerArchive.ReadBytes(length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void Serialize(byte* ptr, int length) => InnerArchive.Serialize(ptr, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] ReadArray<T>(int length) => InnerArchive.ReadArray<T>(length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReadArray<T>(T[] array) => InnerArchive.ReadArray(array);

        public override object Clone() => new FLevelSaveRecordArchive((FArchive) InnerArchive.Clone(), Version);

        public override FName ReadFName() => ReadFString();
    }

    public class FActorTemplateRecord
    {
        public ulong ID;
        public FSoftObjectPath ActorClass;
        public FActorComponentRecord[] ActorComponents;
        public byte[]? ActorData;
        public uint DataHash;

        public FActorTemplateRecord(FLevelSaveRecordArchive Ar)
        {
            ID = Ar.Read<ulong>();

            if (Ar.Version < ELevelSaveRecordVersion.SoftActorClassReferences)
            {
                var obj = Ar.ReadUObject(); // TODO untested code path
                if (obj != null)
                {
                    var pathName = obj.GetPathName();
                    ActorClass = new FSoftObjectPath(pathName.SubstringBeforeLast(':'), ""); // TODO SubPathString
                }
                else
                {
                    ActorClass = new();
                }
            }
            else
            {
                ActorClass = new FSoftObjectPath(Ar);
            }

            // var redirectsObj = GetDefaultObject<ULevelSaveRecordActorClassRedirects>();
            // var redirects = redirectsObj.LevelSaveRecordActorClassRedirects;
            // ... We don't care about redirects anyways

            ActorComponents = Ar.ReadArray(() => new FActorComponentRecord(Ar));

            // skip, idk what it's doing

            ActorData = Ar.ReadArray<byte>();

            var crc = new CRC32();
            crc.SlurpBlock(ActorData, 0, ActorData.Length);
            var hash = (uint) crc.Crc32Result;

            if (Ar.Version < ELevelSaveRecordVersion.AddingDataHash)
            {
                DataHash = hash;
            }
            else
            {
                DataHash = Ar.Read<uint>();
                if (DataHash != hash)
                {
                    Log.Error("FActorTemplateRecord::Serialize failed to deserialize data for: {0} dropping corrupted data.", ActorClass.ToString());
                    ActorData = null;
                    DataHash = 0;
                }
            }
        }
    }

    public class FActorComponentRecord
    {
        public FName ComponentName;
        public FSoftObjectPath ComponentClass; // UClass 
        public byte[]? ComponentData;
        public uint DataHash;

        public FActorComponentRecord(FLevelSaveRecordArchive Ar)
        {
            ComponentName = Ar.ReadFName();

            if (Ar.Version < ELevelSaveRecordVersion.SoftActorComponentClassReferences)
            {
                var obj = Ar.ReadUObject(); // TODO untested code path
                if (obj != null)
                {
                    var pathName = obj.GetPathName();
                    ComponentClass = new FSoftObjectPath(pathName.SubstringBeforeLast(':'), ""); // TODO SubPathString
                }
                else
                {
                    ComponentClass = new();
                }
            }
            else
            {
                ComponentClass = new FSoftObjectPath(Ar);
            }

            // skip, idk what it's doing with /Game/ and /Script/

            ComponentData = Ar.ReadArray<byte>();

            var crc = new CRC32();
            crc.SlurpBlock(ComponentData, 0, ComponentData.Length);
            var hash = (uint) crc.Crc32Result;

            if (Ar.Version < ELevelSaveRecordVersion.AddingDataHash)
            {
                DataHash = hash;
            }
            else
            {
                DataHash = Ar.Read<uint>();
                if (DataHash != hash)
                {
                    Log.Error("FActorComponentRecord::Serialize failed to deserialize data for: {0} dropping corrupted data.", ComponentClass.ToString());
                    ComponentData = null;
                    DataHash = 0;
                }
            }
        }
    }

    public class FActorInstanceRecord
    {
        public ulong RecordID;
        public ulong TemplateRecordID;
        public FName ActorId;
        public FGuid ActorGuid;
        public FTransform Transform;

        public FActorInstanceRecord(FLevelSaveRecordArchive Ar)
        {
            if (Ar.Version < ELevelSaveRecordVersion.TimestampConversion)
            {
                RecordID = Ar.Read<ulong>();
            }
            else
            {
                RecordID = Ar.Read<ulong>(); // There's something stripped out in the above block, so it's the same
            }

            TemplateRecordID = Ar.Read<ulong>();

            if (Ar.Version < ELevelSaveRecordVersion.UsingSaveActorGUID)
            {
                ActorId = Ar.ReadFName();
                // TODO hash or something that results in a unique GUID based on the name?
            }
            else
            {
                if (Ar.Version != ELevelSaveRecordVersion.UsingSaveActorGUID)
                {
                    ActorId = Ar.ReadFName();
                }

                ActorGuid = Ar.Read<FGuid>();
            }

            Transform = Ar.Read<FTransform>();
        }
    }

    public class FLevelStreamedDeleteActorRecord
    {
        public FName ActorId;
        public FTransform Transform;
        public FSoftObjectPath ActorClass; // UClass
        public FSoftObjectPath OwningLevel; // UWorld

        public FLevelStreamedDeleteActorRecord(FAssetArchive Ar)
        {
            ActorId = Ar.ReadFName();
            Transform = Ar.Read<FTransform>();
            ActorClass = new FSoftObjectPath(Ar);
            OwningLevel = new FSoftObjectPath(Ar);
        }
    }

    // /Script/VkEngineTypes.VkModuleVersionRef
    public class FVkModuleVersionRef
    {
        public string ModuleId;
        public string Version;
    }

    public class FFortCreativeVkPalette_ProjectInfo
    {
        public int LinkVersion;
        public int unk;
        public FVkModuleVersionRef[] PublicModules;

        public FFortCreativeVkPalette_ProjectInfo(FArchive Ar)
        {
            LinkVersion = Ar.Read<int>();
        }
    }

    public class FFortCreativeVkPalette
    {
        public Dictionary<string, FFortCreativeVkPalette_ProjectInfo> LinkCodeMap;

        public FFortCreativeVkPalette(FArchive Ar)
        {
            var dummy = Ar.Read<int>();

            var linkCodeMapNum = Ar.Read<int>();
            if (linkCodeMapNum > 0)
            {
                throw new NotImplementedException();
            }
            LinkCodeMap = new Dictionary<string, FFortCreativeVkPalette_ProjectInfo>(linkCodeMapNum);
            for (int i = 0; i < linkCodeMapNum; i++)
            {
                LinkCodeMap[Ar.ReadFString()] = new FFortCreativeVkPalette_ProjectInfo(Ar);
            }
        }
    }

    public class ULevelSaveRecord : UObject
    {
        public FName PackageName;
        public ELevelSaveRecordVersion SaveVersion;
        public bool bCompressed;
        public FVector Center;
        public FVector HalfBoundsExtent;
        public FRotator Rotation;
        public FVector Scale;
        public ulong LastTemplateID; // UnknownData01[0xC]
        public Dictionary<int, FActorTemplateRecord> TemplateRecords;
        public Dictionary<FGuid, FActorInstanceRecord> ActorInstanceRecords;
        public Dictionary<FGuid, FActorInstanceRecord> VolumeInfoActorRecords;
        public Dictionary<FName, FLevelStreamedDeleteActorRecord> LevelStreamedActorsToDelete;
        public int PlayerPersistenceUserWipeNumber;
        public FFortCreativeVkPalette VkPalette;
        public string IslandTemplateId; // UnknownData03[0x48]
        public byte NavmeshRequired; // TODO Find out its enum values
        public bool bRequiresGridPlacement;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            DeserializeHeader(Ar);

            if (SaveVersion < ELevelSaveRecordVersion.SwitchingToCoreSerialization)
            {
                var wrappedAr = new FLevelSaveRecordArchive(Ar, SaveVersion);
                DeserializeLevelSaveRecordData(wrappedAr);
            }
            else
            {
                base.Deserialize(Ar, validPos);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            if (!PackageName.IsNone)
            {
                writer.WritePropertyName("PackageName");
                serializer.Serialize(writer, PackageName);
            }

            writer.WritePropertyName("SaveVersion");
            writer.WriteValue((short) SaveVersion);

            writer.WritePropertyName("bCompressed");
            writer.WriteValue(bCompressed);

            if (IslandTemplateId is { Length: > 0 })
            {
                writer.WritePropertyName("IslandTemplateId");
                writer.WriteValue(IslandTemplateId);
            }

            if (NavmeshRequired != 0)
            {
                writer.WritePropertyName("NavmeshRequired");
                writer.WriteValue(NavmeshRequired);
            }

            base.WriteJson(writer, serializer);

            if (Center != FVector.ZeroVector)
            {
                writer.WritePropertyName("Center");
                serializer.Serialize(writer, Center);
            }

            if (HalfBoundsExtent != FVector.ZeroVector)
            {
                writer.WritePropertyName("HalfBoundsExtent");
                serializer.Serialize(writer, HalfBoundsExtent);
            }

            if (Rotation != FRotator.ZeroRotator)
            {
                writer.WritePropertyName("Rotation");
                serializer.Serialize(writer, Rotation);
            }

            if (LastTemplateID != 0)
            {
                writer.WritePropertyName("LastTemplateID");
                writer.WriteValue(LastTemplateID);
            }

            if (TemplateRecords is { Count: > 0 })
            {
                writer.WritePropertyName("TemplateRecords");
                serializer.Serialize(writer, TemplateRecords);
            }

            if (ActorInstanceRecords is { Count: > 0 })
            {
                writer.WritePropertyName("ActorInstanceRecords");
                serializer.Serialize(writer, ActorInstanceRecords);
            }

            if (VolumeInfoActorRecords is { Count: > 0 })
            {
                writer.WritePropertyName("VolumeInfoActorRecords");
                serializer.Serialize(writer, VolumeInfoActorRecords);
            }

            if (bRequiresGridPlacement)
            {
                writer.WritePropertyName("bRequiresGridPlacement");
                writer.WriteValue(bRequiresGridPlacement);
            }

            if (Scale != FVector.OneVector)
            {
                writer.WritePropertyName("Scale");
                serializer.Serialize(writer, Scale);
            }

            if (LevelStreamedActorsToDelete is { Count: > 0 })
            {
                writer.WritePropertyName("LevelStreamedActorsToDelete");
                serializer.Serialize(writer, LevelStreamedActorsToDelete);
            }

            if (PlayerPersistenceUserWipeNumber != 0)
            {
                writer.WritePropertyName("PlayerPersistenceUserWipeNumber");
                writer.WriteValue(PlayerPersistenceUserWipeNumber);
            }

            // TODO VkPalette
        }

        public void ReadFromArchive(FArchive Ar)
        {
            DeserializeHeader(Ar);
            var data = Ar.ReadArray<byte>();
            FArchive dataAr = bCompressed ? new FArchiveLoadCompressedProxy(Ar.Name, data, "Zlib", versions: Ar.Versions) : new FByteArchive(Ar.Name, data, Ar.Versions);
            var decompressedData = dataAr.ReadArray<byte>();
            var wrappedAr = new FLevelSaveRecordArchive(new FByteArchive(Ar.Name, decompressedData, Ar.Versions), SaveVersion);

            if (SaveVersion >= ELevelSaveRecordVersion.SwitchingToCoreSerialization)
            {
                throw new NotImplementedException();  //base.Deserialize(wrappedAr, -1);
            }
            else
            {
                DeserializeLevelSaveRecordData(wrappedAr);
            }
        }

        private void DeserializeHeader(FArchive Ar)
        {
            PackageName = Ar.ReadFName();
            SaveVersion = Ar.Read<ELevelSaveRecordVersion>();

            if (SaveVersion > ELevelSaveRecordVersion.LatestVersion)
            {
                throw new ParserException("Unsupported level save record version " + (short) SaveVersion);
            }

            bCompressed = Ar.ReadBoolean();

            if (SaveVersion >= ELevelSaveRecordVersion.AddedIslandTemplateId)
            {
                IslandTemplateId = Ar.ReadFString();
            }

            if (SaveVersion >= ELevelSaveRecordVersion.AddedNavmeshRequired)
            {
                NavmeshRequired = Ar.Read<byte>();
            }
        }

        private void DeserializeLevelSaveRecordData(FLevelSaveRecordArchive Ar)
        {
            Center = Ar.Read<FVector>();
            HalfBoundsExtent = Ar.Read<FVector>();
            Rotation = Ar.Read<FRotator>();
            LastTemplateID = Ar.Read<ulong>();

            var numTemplateRecords = Ar.Read<int>();
            TemplateRecords = new Dictionary<int, FActorTemplateRecord>(numTemplateRecords);
            for (int i = 0; i < numTemplateRecords; i++)
            {
                TemplateRecords[Ar.Read<int>()] = new FActorTemplateRecord(Ar);
            }

            var numActorInstanceRecords = Ar.Read<int>();
            ActorInstanceRecords = new Dictionary<FGuid, FActorInstanceRecord>();
            for (int i = 0; i < numActorInstanceRecords; i++)
            {
                ActorInstanceRecords[Ar.Read<FGuid>()] = new FActorInstanceRecord(Ar);
            }

            var numVolumeInfoActorRecords = Ar.Read<int>();
            VolumeInfoActorRecords = new Dictionary<FGuid, FActorInstanceRecord>();
            for (int i = 0; i < numVolumeInfoActorRecords; i++)
            {
                VolumeInfoActorRecords[Ar.Read<FGuid>()] = new FActorInstanceRecord(Ar);
            }

            bRequiresGridPlacement = Ar.ReadBoolean();
            Scale = Ar.Version >= ELevelSaveRecordVersion.AddingScale ? Ar.Read<FVector>() : FVector.OneVector;

            if (Ar.Version >= ELevelSaveRecordVersion.AddedLevelStreamedDeleteRecord)
            {
                var numLevelStreamedActorsToDelete = Ar.Read<int>();
                LevelStreamedActorsToDelete = new Dictionary<FName, FLevelStreamedDeleteActorRecord>();
                for (int i = 0; i < numLevelStreamedActorsToDelete; i++)
                {
                    LevelStreamedActorsToDelete[Ar.ReadFName()] = new FLevelStreamedDeleteActorRecord(Ar);
                }
            }

            if (Ar.Version >= ELevelSaveRecordVersion.AddedPlayerPersistenceUserWipeNumber)
            {
                PlayerPersistenceUserWipeNumber = Ar.Read<int>();
            }

            if (Ar.Version >= ELevelSaveRecordVersion.AddedVkPalette)
            {
                VkPalette = new FFortCreativeVkPalette(Ar);
            }
        }
    }
}