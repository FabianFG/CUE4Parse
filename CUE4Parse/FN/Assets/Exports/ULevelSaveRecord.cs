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
        VersionPlusOne,

        LatestVersion = VersionPlusOne - 1
    }

    public class FLevelSaveRecordArchive : FAssetArchive
    {
        protected readonly FAssetArchive baseArchive;
        public ELevelSaveRecordVersion Version;

        public FLevelSaveRecordArchive(FAssetArchive Ar, ELevelSaveRecordVersion version) : base(Ar, Ar.Owner, Ar.AbsoluteOffset)
        {
            baseArchive = Ar;
            Version = version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count) => baseArchive.Read(buffer, offset, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Seek(long offset, SeekOrigin origin) => baseArchive.Seek(offset, origin);

        public override bool CanSeek => baseArchive.CanSeek;
        public override long Length => baseArchive.Length;
        public override long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => baseArchive.Position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => baseArchive.Position = value;
        }

        public override string Name => baseArchive.Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T Read<T>() => baseArchive.Read<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] ReadBytes(int length) => baseArchive.ReadBytes(length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void Serialize(byte* ptr, int length) => baseArchive.Serialize(ptr, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] ReadArray<T>(int length) => baseArchive.ReadArray<T>(length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReadArray<T>(T[] array) => baseArchive.ReadArray(array);

        public override object Clone() => new FLevelSaveRecordArchive((FAssetArchive) baseArchive.Clone(), Version);

        public override FName ReadFName() => ReadFString();
    }

    public class FActorTemplateRecord
    {
        public ulong unsigned___int640;
        public FSoftObjectPath ActorClass;
        public FActorComponentRecord[] gap30;
        public byte[]? ActorData;
        public uint unsigned_int48;
        public int dword4C;
        public uint DataHash;

        public FActorTemplateRecord(FLevelSaveRecordArchive Ar)
        {
            unsigned___int640 = Ar.Read<ulong>();

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

            gap30 = Ar.ReadArray(() => new FActorComponentRecord(Ar));

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
        public ulong unsigned___int640;
        public ulong unsigned___int648;
        public FName fname10;
        public FGuid fguid18;
        public FTransform transform28;

        public FActorInstanceRecord(FLevelSaveRecordArchive Ar)
        {
            if (Ar.Version < ELevelSaveRecordVersion.TimestampConversion)
            {
                unsigned___int640 = Ar.Read<ulong>();
            }
            else
            {
                unsigned___int640 = Ar.Read<ulong>(); // The same???? What do you mean by timestamp conversion? There's something optimized out in the above block I think
            }

            unsigned___int648 = Ar.Read<ulong>();

            if (Ar.Version < ELevelSaveRecordVersion.UsingSaveActorGUID)
            {
                fname10 = Ar.ReadFName();
                // TODO hash or something that results in a unique GUID based on the name?
            }
            else
            {
                if (Ar.Version != ELevelSaveRecordVersion.UsingSaveActorGUID)
                {
                    fname10 = Ar.ReadFName();
                }

                fguid18 = Ar.Read<FGuid>();
            }

            transform28 = Ar.Read<FTransform>();
        }
    }

    public class FLevelStreamedDeleteActorRecord
    {
        public FName fname;
        public FTransform ftransform;
        public FSoftObjectPath fsoftobjectptr;
        public FSoftObjectPath fsoftobjectptr1;

        public FLevelStreamedDeleteActorRecord(FLevelSaveRecordArchive Ar)
        {
            fname = Ar.ReadFName();
            ftransform = Ar.Read<FTransform>();
            fsoftobjectptr = new FSoftObjectPath(Ar);
            fsoftobjectptr1 = new FSoftObjectPath(Ar);
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
        public ulong Unk0; // UnknownData01[0xC]
        public Dictionary<int, FActorTemplateRecord> TemplateRecords;
        public Dictionary<FGuid, FActorInstanceRecord> ActorInstanceRecords;
        public Dictionary<FGuid, FActorInstanceRecord> VolumeInfoActorRecords;
        public Dictionary<FName, FLevelStreamedDeleteActorRecord> LevelStreamedActorsToDelete;
        public int PlayerPersistenceUserWipeNumber;
        public string IslandTemplateId; // UnknownData03[0x48]
        public bool bRequiresGridPlacement;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            DeserializeHeader(Ar);
            DeserializeInlineLevelSaveRecord(Ar);
        }

        private bool DeserializeHeader(FArchive Ar)
        {
            PackageName = Ar.ReadFName();
            SaveVersion = Ar.Read<ELevelSaveRecordVersion>();

            if (SaveVersion > ELevelSaveRecordVersion.LatestVersion)
            {
                throw new ParserException("Unsupported level save record version " + (short) SaveVersion);
            }
            
            bCompressed = Ar.ReadBoolean();

            if (SaveVersion > ELevelSaveRecordVersion.AddedIslandTemplateId)
            {
                IslandTemplateId = Ar.ReadFString();
            }

            return SaveVersion <= ELevelSaveRecordVersion.AddedPlayerPersistenceUserWipeNumber;
        }

        private void DeserializeInlineLevelSaveRecord(FAssetArchive Ar)
        {
            var wrappedAr = new FLevelSaveRecordArchive(Ar, SaveVersion);
            DeserializeLevelSaveRecordData(wrappedAr);
        }

        private void DeserializeLevelSaveRecordData(FLevelSaveRecordArchive Ar)
        {
            Center = Ar.Read<FVector>();
            HalfBoundsExtent = Ar.Read<FVector>();
            Rotation = Ar.Read<FRotator>();
            Unk0 = Ar.Read<ulong>();

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
            Scale = Ar.Read<FVector>();

            var numLevelStreamedActorsToDelete = Ar.Read<int>();
            LevelStreamedActorsToDelete = new Dictionary<FName, FLevelStreamedDeleteActorRecord>();
            for (int i = 0; i < numLevelStreamedActorsToDelete; i++)
            {
                LevelStreamedActorsToDelete[Ar.ReadFName()] = new FLevelStreamedDeleteActorRecord(Ar);
            }

            PlayerPersistenceUserWipeNumber = Ar.Read<int>();
        }
    }
}