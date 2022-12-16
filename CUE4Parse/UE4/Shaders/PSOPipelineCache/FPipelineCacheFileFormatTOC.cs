using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Shaders
{
    [JsonConverter(typeof(FPipelineCacheFileFormatTOCConverter))]
    public sealed class FPipelineCacheFileFormatTOC
    {
        const ulong FPipelineCacheTOCFileFormatMagic = 0x544F435354415232; // TOCSTAR2
        const ulong FPipelineCacheEOFFileFormatMagic = 0x454F462D4D41524B; // EOF-MARK

        public readonly PSOOrder SortedOrder;
	    public readonly Dictionary<uint, FPipelineCacheFileFormatPSOMetaData> MetaData;

        internal FGuid? OneGUID { get; private set; }

        public FPipelineCacheFileFormatTOC(FArchive Ar, ref FPipelineCacheFileFormatHeader PSOheader)
        {
            Ar.Position = (long)PSOheader.TableOffset;
            ulong TOCMagic = Ar.Read<ulong>();
            long tocPos = Ar.Position;
            Ar.Position = Ar.Length - sizeof(ulong);
            ulong EOCMagic = Ar.Read<ulong>();

            if (TOCMagic != FPipelineCacheTOCFileFormatMagic || EOCMagic != FPipelineCacheEOFFileFormatMagic)
                throw new ParserException($"Invalid or unsupported {nameof(FPipelineCacheFileFormatTOC)} data!");

            Ar.Position = tocPos;
            FGuid? FirstEntryGuid = null;
            byte serAllEntriesUseSameGuid = Ar.Read<byte>();
            bool bAllEntriesUseSameGuid = serAllEntriesUseSameGuid != 0 ? true : false;
            if (bAllEntriesUseSameGuid)
            {
                FirstEntryGuid = Ar.Read<FGuid>();
            }
            OneGUID = FirstEntryGuid;
            SortedOrder = (PSOOrder)Ar.Read<uint>();
            int metaEntries = Ar.Read<int>();
            MetaData = new Dictionary<uint, FPipelineCacheFileFormatPSOMetaData>(metaEntries);
            for (int i = 0; i < metaEntries; i++)
            {
                uint Key = Ar.Read<uint>();
                var PSOmeta = new FPipelineCacheFileFormatPSOMetaData(Ar, ref PSOheader);
                if (bAllEntriesUseSameGuid)
                    PSOmeta.FileGuid = FirstEntryGuid;
                MetaData[Key] = PSOmeta;
            }
        }
    }

    public struct FPipelineCacheFileFormatPSOMetaData
    {
        public ulong  FileOffset;
	    public ulong  FileSize;
	    public FGuid? FileGuid;
	    public FPipelineStateStats Stats;
	    public FSHAHash[]? Shaders;
	    public ulong?  UsageMask;
	    public long?   LastUsedUnixTime;
	    public ushort? EngineFlags;

        public FPipelineCacheFileFormatPSOMetaData(FArchive Ar, ref FPipelineCacheFileFormatHeader PSOHeader)
        {
            FileOffset = Ar.Read<ulong>();
            FileSize = Ar.Read<ulong>();

            byte ArchiveFullGuid = 1;
            if (PSOHeader.Version == (uint)EPipelineCacheFileFormatVersions.PatchSizeReduction_NoDuplicatedGuid)
                ArchiveFullGuid = Ar.Read<byte>();

            if (ArchiveFullGuid != 0)
                FileGuid = Ar.Read<FGuid>();

            Stats = Ar.Read<FPipelineStateStats>();

            if (PSOHeader.Version == (uint)EPipelineCacheFileFormatVersions.LibraryID)
            {
                //uint[] IDs;
                int count = Ar.Read<int>();
                Ar.Position = Ar.Position + count * sizeof(uint);
            }
            else if (PSOHeader.Version >= (uint)EPipelineCacheFileFormatVersions.ShaderMetaData)
            {
                Shaders = Ar.ReadArray(() => new FSHAHash(Ar));
            }

            if (PSOHeader.Version >= (uint)EPipelineCacheFileFormatVersions.PSOUsageMask)
                UsageMask = Ar.Read<ulong>();

            if (PSOHeader.Version >= (uint)EPipelineCacheFileFormatVersions.EngineFlags)
                EngineFlags = Ar.Read<ushort>();

            if (PSOHeader.Version >= (uint)EPipelineCacheFileFormatVersions.LastUsedTime)
                LastUsedUnixTime = Ar.Read<long>();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FPipelineStateStats
    {
        public readonly long FirstFrameUsed;
	    public readonly long LastFrameUsed;
	    public readonly ulong CreateCount;
	    public readonly long TotalBindCount;
	    public readonly uint PSOHash;
    }

    public class FPipelineCacheFileFormatTOCConverter : JsonConverter<FPipelineCacheFileFormatTOC>
	{
		public override void WriteJson(JsonWriter writer, FPipelineCacheFileFormatTOC value, JsonSerializer serializer)
		{
			writer.WritePropertyName("Content");
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(FPipelineCacheFileFormatTOC.SortedOrder));
            writer.WriteValue(value.SortedOrder);
            if (value.OneGUID.HasValue)
            {
                writer.WritePropertyName("SharedGUID");
                serializer.Serialize(writer, value.OneGUID.Value);
            }
            writer.WritePropertyName(nameof(FPipelineCacheFileFormatTOC.MetaData));

            writer.WriteStartArray();
            foreach (var entry in value.MetaData)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("FileSize");
                writer.WriteValue(entry.Value.FileSize);
                writer.WritePropertyName("FileOffset");
                writer.WriteValue(entry.Value.FileOffset);

                if (!value.OneGUID.HasValue && entry.Value.FileGuid.HasValue)
                {
                    writer.WritePropertyName("FileGUID");
                    writer.WriteValue(entry.Value.FileGuid);
                }

                writer.WritePropertyName("Stats");
                writer.WriteStartObject();

                writer.WritePropertyName("FirstFrameUsed");
                writer.WriteValue(entry.Value.Stats.FirstFrameUsed);
                writer.WritePropertyName("LastFrameUsed");
                writer.WriteValue(entry.Value.Stats.LastFrameUsed);
                writer.WritePropertyName("CreateCount");
                writer.WriteValue(entry.Value.Stats.CreateCount);
                writer.WritePropertyName("TotalBindCount");
                writer.WriteValue(entry.Value.Stats.TotalBindCount);
                writer.WritePropertyName("PSOHash");
                writer.WriteValue(entry.Value.Stats.PSOHash);

                writer.WriteEndObject();

                if (entry.Value.Shaders?.Length > 0)
                {
                    writer.WritePropertyName("Shaders");
                    writer.WriteStartArray();
                    for (int i = 0; i < entry.Value.Shaders.Length; i++)
                        writer.WriteValue(entry.Value.Shaders[i].ToString());
                    writer.WriteEndArray();
                }
                if (entry.Value.UsageMask.HasValue)
                {
                    writer.WritePropertyName("UsageMask");
                    writer.WriteValue(entry.Value.UsageMask);
                }
                if (entry.Value.EngineFlags.HasValue)
                {
                    writer.WritePropertyName("EngineFlags");
                    writer.WriteValue(entry.Value.EngineFlags);
                }
                if (entry.Value.LastUsedUnixTime.HasValue)
                {
                    writer.WritePropertyName("LastUsedUnixTime");
                    writer.WriteValue(entry.Value.LastUsedUnixTime);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
		}

		public override FPipelineCacheFileFormatTOC ReadJson(JsonReader reader, Type objectType, FPipelineCacheFileFormatTOC existingValue, bool hasExistingValue,
			JsonSerializer serializer)
			=> throw new NotImplementedException();
	}
}