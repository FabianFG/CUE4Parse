using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Serilog;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [JsonConverter(typeof(USkeletonConverter))]
    public class USkeleton : UObject
    {
        public FBoneNode[] BoneTree { get; private set; }
        public FReferenceSkeleton ReferenceSkeleton { get; private set; }
        public FGuid Guid { get; private set; }
        public FGuid VirtualBoneGuid { get; private set; }
        public Dictionary<FName, FReferencePose> AnimRetargetSources { get; private set; }
        public Dictionary<FName, FSmartNameMapping> NameMappings { get; private set; }
        public FName[] ExistingMarkerNames { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            // UObject Properties
            BoneTree = GetOrDefault<FBoneNode[]>(nameof(BoneTree));
            VirtualBoneGuid = GetOrDefault<FGuid>(nameof(VirtualBoneGuid));

            if (Ar.Ver >= UE4Version.VER_UE4_REFERENCE_SKELETON_REFACTOR)
            {
                ReferenceSkeleton = new FReferenceSkeleton(Ar);
            }

            if (Ar.Ver >= UE4Version.VER_UE4_FIX_ANIMATIONBASEPOSE_SERIALIZATION)
            {
                var numOfRetargetSources = Ar.Read<int>();
                AnimRetargetSources = new Dictionary<FName, FReferencePose>(numOfRetargetSources);
                for (var i = 0; i < numOfRetargetSources; i++)
                {
                    AnimRetargetSources[Ar.ReadFName()] = new FReferencePose(Ar);
                }
            }
            else
            {
                Log.Warning(""); // not sure what to put here
            }

            if (Ar.Ver >= UE4Version.VER_UE4_SKELETON_GUID_SERIALIZATION)
            {
                Guid = Ar.Read<FGuid>();
            }

            if (Ar.Ver >= UE4Version.VER_UE4_SKELETON_ADD_SMARTNAMES)
            {
                var mapLength = Ar.Read<int>();
                NameMappings = new Dictionary<FName, FSmartNameMapping>(mapLength);
                for (var i = 0; i < mapLength; i++)
                {
                    NameMappings[Ar.ReadFName()] = new FSmartNameMapping(Ar);
                }
            }

            if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.StoreMarkerNamesOnSkeleton)
            {
                var stripDataFlags = Ar.Read<FStripDataFlags>();
                if (!stripDataFlags.IsEditorDataStripped())
                {
                    ExistingMarkerNames = Ar.ReadArray(Ar.Read<int>(), Ar.ReadFName);
                }
            }
        }
    }
    
    public class USkeletonConverter : JsonConverter<USkeleton>
    {
        public override void WriteJson(JsonWriter writer, USkeleton value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }
            
            // export properties
            writer.WritePropertyName("Properties");
            writer.WriteStartObject();
            {
                writer.WritePropertyName("ReferenceSkeleton");
                serializer.Serialize(writer, value.ReferenceSkeleton);
                
                writer.WritePropertyName("Guid");
                serializer.Serialize(writer, value.Guid);
                
                writer.WritePropertyName("AnimRetargetSources");
                serializer.Serialize(writer, value.AnimRetargetSources);

                writer.WritePropertyName("NameMappings");
                serializer.Serialize(writer, value.NameMappings);

                writer.WritePropertyName("ExistingMarkerNames");
                serializer.Serialize(writer, value.ExistingMarkerNames);

                foreach (var property in value.Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }
            }
            writer.WriteEndObject();
            
            writer.WriteEndObject();
        }

        public override USkeleton ReadJson(JsonReader reader, Type objectType, USkeleton existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
