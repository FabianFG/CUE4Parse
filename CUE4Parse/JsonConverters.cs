using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FF7.Objects;
using CUE4Parse.GameTypes.FN.Objects;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Animation.ACL;
using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Kismet;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.Engine.GameFramework;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.WorldCondition;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Wwise;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.Utils;
using Newtonsoft.Json;
#pragma warning disable CS8765

namespace CUE4Parse;

public class FTextConverter : JsonConverter<FText>
{
    public override void WriteJson(JsonWriter writer, FText value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.TextHistory);
    }

    public override FText ReadJson(JsonReader reader, Type objectType, FText existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FCurveMetaDataConverter : JsonConverter<FCurveMetaData>
{
    public override void WriteJson(JsonWriter writer, FCurveMetaData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Type");
        serializer.Serialize(writer, value.Type);

        writer.WritePropertyName("LinkedBones");
        writer.WriteStartArray();
        foreach (var bone in value.LinkedBones)
        {
            serializer.Serialize(writer, bone);
        }
        writer.WriteEndArray();

        writer.WritePropertyName("MaxLOD");
        writer.WriteValue(value.MaxLOD);

        writer.WriteEndObject();
    }

    public override FCurveMetaData ReadJson(JsonReader reader, Type objectType, FCurveMetaData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FIoStoreShaderCodeArchiveConverter : JsonConverter<FIoStoreShaderCodeArchive>
{
    public override void WriteJson(JsonWriter writer, FIoStoreShaderCodeArchive value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("ShaderMapHashes");
        writer.WriteStartArray();
        foreach (var shaderMapHash in value.ShaderMapHashes)
        {
            serializer.Serialize(writer, shaderMapHash.Hash);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("ShaderHashes");
        writer.WriteStartArray();
        foreach (var shaderHash in value.ShaderHashes)
        {
            serializer.Serialize(writer, shaderHash.Hash);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("ShaderGroupIoHashes");
        serializer.Serialize(writer, value.ShaderGroupIoHashes);

        writer.WritePropertyName("ShaderMapEntries");
        serializer.Serialize(writer, value.ShaderMapEntries);

        writer.WritePropertyName("ShaderEntries");
        serializer.Serialize(writer, value.ShaderEntries);

        writer.WritePropertyName("ShaderGroupEntries");
        serializer.Serialize(writer, value.ShaderGroupEntries);

        writer.WritePropertyName("ShaderIndices");
        serializer.Serialize(writer, value.ShaderIndices);

        writer.WriteEndObject();
    }

    public override FIoStoreShaderCodeArchive ReadJson(JsonReader reader, Type objectType, FIoStoreShaderCodeArchive existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FACLDatabaseCompressedAnimDataConverter : JsonConverter<FACLDatabaseCompressedAnimData>
{
    public override void WriteJson(JsonWriter writer, FACLDatabaseCompressedAnimData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("CompressedNumberOfFrames");
        writer.WriteValue(value.CompressedNumberOfFrames);

        writer.WritePropertyName("SequenceNameHash");
        writer.WriteValue(value.SequenceNameHash);

        writer.WriteEndObject();
    }

    public override FACLDatabaseCompressedAnimData ReadJson(JsonReader reader, Type objectType, FACLDatabaseCompressedAnimData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSerializedShaderArchiveConverter : JsonConverter<FSerializedShaderArchive>
{
    public override void WriteJson(JsonWriter writer, FSerializedShaderArchive value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("ShaderMapHashes");
        writer.WriteStartArray();
        foreach (var shaderMapHash in value.ShaderMapHashes)
        {
            serializer.Serialize(writer, shaderMapHash.Hash);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("ShaderHashes");
        writer.WriteStartArray();
        foreach (var shaderHash in value.ShaderHashes)
        {
            serializer.Serialize(writer, shaderHash.Hash);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("ShaderMapEntries");
        serializer.Serialize(writer, value.ShaderMapEntries);

        writer.WritePropertyName("ShaderEntries");
        serializer.Serialize(writer, value.ShaderEntries);

        writer.WritePropertyName("PreloadEntries");
        serializer.Serialize(writer, value.PreloadEntries);

        writer.WritePropertyName("ShaderIndices");
        serializer.Serialize(writer, value.ShaderIndices);

        writer.WriteEndObject();
    }

    public override FSerializedShaderArchive ReadJson(JsonReader reader, Type objectType, FSerializedShaderArchive existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FUniqueNetIdReplConverter : JsonConverter<FUniqueNetIdRepl>
{
    public override void WriteJson(JsonWriter writer, FUniqueNetIdRepl value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.UniqueNetId != null ? value.UniqueNetId : "INVALID");
    }

    public override FUniqueNetIdRepl ReadJson(JsonReader reader, Type objectType, FUniqueNetIdRepl existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FColorVertexBufferConverter : JsonConverter<FColorVertexBuffer>
{
    public override void WriteJson(JsonWriter writer, FColorVertexBuffer value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        // writer.WritePropertyName("Data");
        // serializer.Serialize(writer, value.Data);

        writer.WritePropertyName("Stride");
        writer.WriteValue(value.Stride);

        writer.WritePropertyName("NumVertices");
        writer.WriteValue(value.NumVertices);

        writer.WriteEndObject();
    }

    public override FColorVertexBuffer ReadJson(JsonReader reader, Type objectType, FColorVertexBuffer existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FShaderCodeArchiveConverter : JsonConverter<FShaderCodeArchive>
{
    public override void WriteJson(JsonWriter writer, FShaderCodeArchive value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("SerializedShaders");
        serializer.Serialize(writer, value.SerializedShaders);

        // TODO: Try to read this as actual data.
        // writer.WritePropertyName("ShaderCode");
        // serializer.Serialize(writer, value.ShaderCode);

        writer.WriteEndObject();
    }

    public override FShaderCodeArchive ReadJson(JsonReader reader, Type objectType, FShaderCodeArchive existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStructFallbackConverter : JsonConverter<FStructFallback>
{
    public override void WriteJson(JsonWriter writer, FStructFallback value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        foreach (var property in value.Properties)
        {
            writer.WritePropertyName(property.ArrayIndex > 0 ? $"{property.Name.Text}[{property.ArrayIndex}]" : property.Name.Text);
            serializer.Serialize(writer, property.Tag);
        }

        writer.WriteEndObject();
    }

    public override FStructFallback ReadJson(JsonReader reader, Type objectType, FStructFallback existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FPositionVertexBufferConverter : JsonConverter<FPositionVertexBuffer>
{
    public override void WriteJson(JsonWriter writer, FPositionVertexBuffer value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        // writer.WritePropertyName("Verts");
        // serializer.Serialize(writer, value.Verts);

        writer.WritePropertyName("Stride");
        writer.WriteValue(value.Stride);

        writer.WritePropertyName("NumVertices");
        writer.WriteValue(value.NumVertices);

        writer.WriteEndObject();
    }

    public override FPositionVertexBuffer ReadJson(JsonReader reader, Type objectType, FPositionVertexBuffer existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FUECompressedAnimDataConverter : JsonConverter<FUECompressedAnimData>
{
    public override void WriteJson(JsonWriter writer, FUECompressedAnimData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (value.CompressedNumberOfFrames > 0)
        {
            writer.WritePropertyName("CompressedNumberOfFrames");
            writer.WriteValue(value.CompressedNumberOfFrames);
        }

        writer.WritePropertyName("KeyEncodingFormat");
        writer.WriteValue(value.KeyEncodingFormat.ToString());

        writer.WritePropertyName("TranslationCompressionFormat");
        writer.WriteValue(value.TranslationCompressionFormat.ToString());

        writer.WritePropertyName("RotationCompressionFormat");
        writer.WriteValue(value.RotationCompressionFormat.ToString());

        writer.WritePropertyName("ScaleCompressionFormat");
        writer.WriteValue(value.ScaleCompressionFormat.ToString());

        /*writer.WritePropertyName("CompressedByteStream");
        writer.WriteValue(value.CompressedByteStream);

        writer.WritePropertyName("CompressedTrackOffsets");
        serializer.Serialize(writer, value.CompressedTrackOffsets);

        writer.WritePropertyName("CompressedScaleOffsets");
        writer.WriteStartObject();
        {
            writer.WritePropertyName("OffsetData");
            serializer.Serialize(writer, value.CompressedScaleOffsets.OffsetData);

            writer.WritePropertyName("StripSize");
            writer.WriteValue(value.CompressedScaleOffsets.StripSize);
        }
        writer.WriteEndObject();*/

        writer.WriteEndObject();
    }

    public override FUECompressedAnimData ReadJson(JsonReader reader, Type objectType, FUECompressedAnimData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class ArrayPropertyConverter : JsonConverter<ArrayProperty>
{
    public override void WriteJson(JsonWriter writer, ArrayProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override ArrayProperty ReadJson(JsonReader reader, Type objectType, ArrayProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class AssetObjectPropertyConverter : JsonConverter<AssetObjectProperty>
{
    public override void WriteJson(JsonWriter writer, AssetObjectProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override AssetObjectProperty ReadJson(JsonReader reader, Type objectType, AssetObjectProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class BoolPropertyConverter : JsonConverter<BoolProperty>
{
    public override void WriteJson(JsonWriter writer, BoolProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override BoolProperty ReadJson(JsonReader reader, Type objectType, BoolProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class BytePropertyConverter : JsonConverter<ByteProperty>
{
    public override void WriteJson(JsonWriter writer, ByteProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override ByteProperty ReadJson(JsonReader reader, Type objectType, ByteProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class DelegatePropertyConverter : JsonConverter<DelegateProperty>
{
    public override void WriteJson(JsonWriter writer, DelegateProperty value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Num");
        writer.WriteValue(value.Num);

        writer.WritePropertyName("Name");
        serializer.Serialize(writer, value.Value);

        writer.WriteEndObject();
    }

    public override DelegateProperty ReadJson(JsonReader reader, Type objectType, DelegateProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class DoublePropertyConverter : JsonConverter<DoubleProperty>
{
    public override void WriteJson(JsonWriter writer, DoubleProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override DoubleProperty ReadJson(JsonReader reader, Type objectType, DoubleProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class EnumPropertyConverter : JsonConverter<EnumProperty>
{
    public override void WriteJson(JsonWriter writer, EnumProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override EnumProperty ReadJson(JsonReader reader, Type objectType, EnumProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FieldPathPropertyConverter : JsonConverter<FieldPathProperty>
{
    public override void WriteJson(JsonWriter writer, FieldPathProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override FieldPathProperty ReadJson(JsonReader reader, Type objectType, FieldPathProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FloatPropertyConverter : JsonConverter<FloatProperty>
{
    public override void WriteJson(JsonWriter writer, FloatProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override FloatProperty ReadJson(JsonReader reader, Type objectType, FloatProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class Int16PropertyConverter : JsonConverter<Int16Property>
{
    public override void WriteJson(JsonWriter writer, Int16Property value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override Int16Property ReadJson(JsonReader reader, Type objectType, Int16Property existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class Int64PropertyConverter : JsonConverter<Int64Property>
{
    public override void WriteJson(JsonWriter writer, Int64Property value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override Int64Property ReadJson(JsonReader reader, Type objectType, Int64Property existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class InterfacePropertyConverter : JsonConverter<InterfaceProperty>
{
    public override void WriteJson(JsonWriter writer, InterfaceProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override InterfaceProperty ReadJson(JsonReader reader, Type objectType, InterfaceProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class Int8PropertyConverter : JsonConverter<Int8Property>
{
    public override void WriteJson(JsonWriter writer, Int8Property value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override Int8Property ReadJson(JsonReader reader, Type objectType, Int8Property existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class IntPropertyConverter : JsonConverter<IntProperty>
{
    public override void WriteJson(JsonWriter writer, IntProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override IntProperty ReadJson(JsonReader reader, Type objectType, IntProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class LazyObjectPropertyConverter : JsonConverter<LazyObjectProperty>
{
    public override void WriteJson(JsonWriter writer, LazyObjectProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override LazyObjectProperty ReadJson(JsonReader reader, Type objectType, LazyObjectProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class NamePropertyConverter : JsonConverter<NameProperty>
{
    public override void WriteJson(JsonWriter writer, NameProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override NameProperty ReadJson(JsonReader reader, Type objectType, NameProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class ObjectPropertyConverter : JsonConverter<ObjectProperty>
{
    public override void WriteJson(JsonWriter writer, ObjectProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override ObjectProperty ReadJson(JsonReader reader, Type objectType, ObjectProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class OptionalPropertyConverter : JsonConverter<OptionalProperty>
{
    public override void WriteJson(JsonWriter writer, OptionalProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override OptionalProperty ReadJson(JsonReader reader, Type objectType, OptionalProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class SetPropertyConverter : JsonConverter<SetProperty>
{
    public override void WriteJson(JsonWriter writer, SetProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override SetProperty ReadJson(JsonReader reader, Type objectType, SetProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class MapPropertyConverter : JsonConverter<MapProperty>
{
    public override void WriteJson(JsonWriter writer, MapProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override MapProperty ReadJson(JsonReader reader, Type objectType, MapProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class SoftObjectPropertyConverter : JsonConverter<SoftObjectProperty>
{
    public override void WriteJson(JsonWriter writer, SoftObjectProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override SoftObjectProperty ReadJson(JsonReader reader, Type objectType, SoftObjectProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class MulticastDelegatePropertyConverter : JsonConverter<MulticastDelegateProperty>
{
    public override void WriteJson(JsonWriter writer, MulticastDelegateProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override MulticastDelegateProperty ReadJson(JsonReader reader, Type objectType, MulticastDelegateProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class StrPropertyConverter : JsonConverter<StrProperty>
{
    public override void WriteJson(JsonWriter writer, StrProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override StrProperty ReadJson(JsonReader reader, Type objectType, StrProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class VerseStringPropertyConverter : JsonConverter<VerseStringProperty>
{
    public override void WriteJson(JsonWriter writer, VerseStringProperty value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override VerseStringProperty ReadJson(JsonReader reader, Type objectType, VerseStringProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class StructPropertyConverter : JsonConverter<StructProperty>
{
    public override void WriteJson(JsonWriter writer, StructProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override StructProperty ReadJson(JsonReader reader, Type objectType, StructProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class TextPropertyConverter : JsonConverter<TextProperty>
{
    public override void WriteJson(JsonWriter writer, TextProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override TextProperty ReadJson(JsonReader reader, Type objectType, TextProperty existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UInt16PropertyConverter : JsonConverter<UInt16Property>
{
    public override void WriteJson(JsonWriter writer, UInt16Property value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override UInt16Property ReadJson(JsonReader reader, Type objectType, UInt16Property existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UInt64PropertyConverter : JsonConverter<UInt64Property>
{
    public override void WriteJson(JsonWriter writer, UInt64Property value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override UInt64Property ReadJson(JsonReader reader, Type objectType, UInt64Property existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UInt32PropertyConverter : JsonConverter<UInt32Property>
{
    public override void WriteJson(JsonWriter writer, UInt32Property value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }

    public override UInt32Property ReadJson(JsonReader reader, Type objectType, UInt32Property existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FFieldConverter : JsonConverter<FField>
{
    public override void WriteJson(JsonWriter writer, FField value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        value.WriteJson(writer, serializer);
        writer.WriteEndObject();
    }

    public override FField ReadJson(JsonReader reader, Type objectType, FField existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FScriptInterfaceConverter : JsonConverter<FScriptInterface>
{
    public override void WriteJson(JsonWriter writer, FScriptInterface value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Object);
    }

    public override FScriptInterface ReadJson(JsonReader reader, Type objectType, FScriptInterface existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UObjectConverter : JsonConverter<UObject>
{
    public override void WriteJson(JsonWriter writer, UObject value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        value.WriteJson(writer, serializer);
        writer.WriteEndObject();
    }

    public override UObject ReadJson(JsonReader reader, Type objectType, UObject existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FPackageFileSummaryConverter : JsonConverter<FPackageFileSummary>
{
    public override void WriteJson(JsonWriter writer, FPackageFileSummary value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Tag));
        writer.WriteValue(value.Tag.ToString("X8"));

        writer.WritePropertyName(nameof(value.PackageFlags));
        writer.WriteValue(value.PackageFlags.ToStringBitfield());

        writer.WritePropertyName(nameof(value.TotalHeaderSize));
        writer.WriteValue(value.TotalHeaderSize);

        writer.WritePropertyName(nameof(value.NameOffset));
        writer.WriteValue(value.NameOffset);

        writer.WritePropertyName(nameof(value.NameCount));
        writer.WriteValue(value.NameCount);

        writer.WritePropertyName(nameof(value.ImportOffset));
        writer.WriteValue(value.ImportOffset);

        writer.WritePropertyName(nameof(value.ImportCount));
        writer.WriteValue(value.ImportCount);

        writer.WritePropertyName(nameof(value.ExportOffset));
        writer.WriteValue(value.ExportOffset);

        writer.WritePropertyName(nameof(value.ExportCount));
        writer.WriteValue(value.ExportCount);

        writer.WritePropertyName(nameof(value.BulkDataStartOffset));
        writer.WriteValue(value.BulkDataStartOffset);

        writer.WritePropertyName(nameof(value.FileVersionUE));
        writer.WriteValue(value.FileVersionUE.ToString());

        writer.WritePropertyName(nameof(value.FileVersionLicenseeUE));
        writer.WriteValue(value.FileVersionLicenseeUE.ToStringBitfield());

        writer.WritePropertyName("CustomVersions");
        serializer.Serialize(writer, value.CustomVersionContainer.Versions);

        writer.WritePropertyName(nameof(value.bUnversioned));
        writer.WriteValue(value.bUnversioned);

        writer.WriteEndObject();
    }

    public override FPackageFileSummary ReadJson(JsonReader reader, Type objectType, FPackageFileSummary existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class PackageConverter : JsonConverter<Package>
{
    public override void WriteJson(JsonWriter writer, Package value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Summary));
        serializer.Serialize(writer, value.Summary);

        writer.WritePropertyName(nameof(value.NameMap));
        writer.WriteStartArray();
        foreach (var name in value.NameMap)
        {
            writer.WriteValue(name.Name);
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(value.ImportMap));
        writer.WriteStartArray();
        for (var i = 0; i < value.ImportMap.Length; i++)
        {
            serializer.Serialize(writer, new FPackageIndex(value, -i - 1));
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(value.ExportMap));
        writer.WriteStartArray();
        for (var i = 0; i < value.ExportMap.Length; i++)
        {
            serializer.Serialize(writer, new FPackageIndex(value, i + 1));
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public override Package ReadJson(JsonReader reader, Type objectType, Package existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class IoPackageConverter : JsonConverter<IoPackage>
{
    public override void WriteJson(JsonWriter writer, IoPackage value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Summary));
        serializer.Serialize(writer, value.Summary);

        writer.WritePropertyName(nameof(value.NameMap));
        writer.WriteStartArray();
        foreach (var name in value.NameMap)
        {
            writer.WriteValue(name.Name);
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(value.ImportMap));
        writer.WriteStartArray();
        for (var i = 0; i < value.ImportMap.Length; i++)
        {
            serializer.Serialize(writer, new FPackageIndex(value, -i - 1));
        }
        writer.WriteEndArray();

        writer.WritePropertyName(nameof(value.ExportMap));
        writer.WriteStartArray();
        for (var i = 0; i < value.ExportMap.Length; i++)
        {
            serializer.Serialize(writer, new FPackageIndex(value, i + 1));
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public override IoPackage ReadJson(JsonReader reader, Type objectType, IoPackage existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FPackageIndexConverter : JsonConverter<FPackageIndex>
{
    public override void WriteJson(JsonWriter writer, FPackageIndex value, JsonSerializer serializer)
    {
        #region V3
        serializer.Serialize(writer, value.ResolvedObject);
        #endregion

        #region V2
        // var resolved = value.Owner?.ResolvePackageIndex(value);
        // if (resolved != null)
        // {
        //     var outerChain = new List<string>();
        //     var current = resolved;
        //     while (current != null)
        //     {
        //         outerChain.Add(current.Name.Text);
        //         current = current.Outer;
        //     }
        //
        //     var sb = new StringBuilder(256);
        //     for (int i = 1; i <= outerChain.Count; i++)
        //     {
        //         var name = outerChain[outerChain.Count - i];
        //         sb.Append(name);
        //         if (i < outerChain.Count)
        //         {
        //             sb.Append(i > 1 ? ":" : ".");
        //         }
        //     }
        //
        //     writer.WriteValue($"{resolved.Class?.Name}'{sb}'");
        // }
        // else
        // {
        //     writer.WriteValue("None");
        // }
        #endregion

        #region V1
        // if (value.ImportObject != null)
        // {
        //     serializer.Serialize(writer, value.ImportObject);
        // }
        // else if (value.ExportObject != null)
        // {
        //     serializer.Serialize(writer, value.ExportObject);
        // }
        // else
        // {
        //     writer.WriteValue(value.Index);
        // }
        #endregion
    }

    public override FPackageIndex ReadJson(JsonReader reader, Type objectType, FPackageIndex existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FObjectResourceConverter : JsonConverter<FObjectResource>
{
    public override void WriteJson(JsonWriter writer, FObjectResource value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case FObjectImport i:
                writer.WritePropertyName("ObjectName");
                writer.WriteValue($"{i.ObjectName.Text}:{i.ClassName.Text}");
                break;
            case FObjectExport e:
                writer.WritePropertyName("ObjectName");
                writer.WriteValue($"{e.ObjectName.Text}:{e.ClassName}");
                break;
        }

        writer.WritePropertyName("OuterIndex");
        serializer.Serialize(writer, value.OuterIndex);

        writer.WriteEndObject();
    }

    public override FObjectResource ReadJson(JsonReader reader, Type objectType, FObjectResource existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FPropertyTagTypeConverter : JsonConverter<FPropertyTagType>
{
    public override void WriteJson(JsonWriter writer, FPropertyTagType value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override FPropertyTagType ReadJson(JsonReader reader, Type objectType, FPropertyTagType existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FScriptStructConverter : JsonConverter<FScriptStruct>
{
    public override void WriteJson(JsonWriter writer, FScriptStruct value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.StructType);
    }

    public override FScriptStruct ReadJson(JsonReader reader, Type objectType, FScriptStruct existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UScriptSetConverter : JsonConverter<UScriptSet>
{
    public override void WriteJson(JsonWriter writer, UScriptSet value, JsonSerializer serializer)
    {
        writer.WriteStartArray();

        foreach (var property in value.Properties)
        {
            serializer.Serialize(writer, property);
        }

        writer.WriteEndArray();
    }

    public override UScriptSet ReadJson(JsonReader reader, Type objectType, UScriptSet existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UScriptMapConverter : JsonConverter<UScriptMap>
{
    public override void WriteJson(JsonWriter writer, UScriptMap value, JsonSerializer serializer)
    {
        writer.WriteStartArray();

        foreach (var kvp in value.Properties)
        {
            writer.WriteStartObject();
            switch (kvp.Key)
            {
                case StructProperty:
                    writer.WritePropertyName("Key");
                    serializer.Serialize(writer, kvp.Key);
                    writer.WritePropertyName("Value");
                    serializer.Serialize(writer, kvp.Value);
                    break;
                default:
                    writer.WritePropertyName("Key");
                    writer.WriteValue(kvp.Key.ToString().SubstringBefore('(').Trim());
                    writer.WritePropertyName("Value");
                    serializer.Serialize(writer, kvp.Value);
                    break;
            }
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public override UScriptMap ReadJson(JsonReader reader, Type objectType, UScriptMap existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UScriptArrayConverter : JsonConverter<UScriptArray>
{
    public override void WriteJson(JsonWriter writer, UScriptArray value, JsonSerializer serializer)
    {
        writer.WriteStartArray();

        foreach (var property in value.Properties)
        {
            serializer.Serialize(writer, property);
        }

        writer.WriteEndArray();
    }

    public override UScriptArray ReadJson(JsonReader reader, Type objectType, UScriptArray existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class AkEntryConverter : JsonConverter<AkEntry>
{
    public override void WriteJson(JsonWriter writer, AkEntry value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("NameHash");
        writer.WriteValue(value.NameHash);

        writer.WritePropertyName("OffsetMultiplier");
        writer.WriteValue(value.OffsetMultiplier);

        writer.WritePropertyName("Size");
        writer.WriteValue(value.Size);

        writer.WritePropertyName("Offset");
        writer.WriteValue(value.Offset);

        writer.WritePropertyName("FolderId");
        writer.WriteValue(value.FolderId);

        writer.WritePropertyName("Path");
        writer.WriteValue(value.Path);

        writer.WritePropertyName("IsSoundBank");
        writer.WriteValue(value.IsSoundBank);

        writer.WriteEndObject();
    }

    public override AkEntry ReadJson(JsonReader reader, Type objectType, AkEntry existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class AkFolderConverter : JsonConverter<AkFolder>
{
    public override void WriteJson(JsonWriter writer, AkFolder value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Offset");
        writer.WriteValue(value.Offset);

        writer.WritePropertyName("Id");
        writer.WriteValue(value.Id);

        writer.WritePropertyName("Name");
        writer.WriteValue(value.Name);

        writer.WritePropertyName("Entries");
        serializer.Serialize(writer, value.Entries);

        writer.WriteEndObject();
    }

    public override AkFolder ReadJson(JsonReader reader, Type objectType, AkFolder existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FAkMediaDataChunkConverter : JsonConverter<FAkMediaDataChunk>
{
    public override void WriteJson(JsonWriter writer, FAkMediaDataChunk value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("BulkData");
        serializer.Serialize(writer, value.Data);

        writer.WritePropertyName("IsPrefetch");
        writer.WriteValue(value.IsPrefetch);

        writer.WriteEndObject();
    }

    public override FAkMediaDataChunk ReadJson(JsonReader reader, Type objectType, FAkMediaDataChunk existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FPackedNormalConverter : JsonConverter<FPackedNormal>
{
    public override void WriteJson(JsonWriter writer, FPackedNormal value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Data");
        writer.WriteValue(value.Data);

        writer.WriteEndObject();
    }

    public override FPackedNormal ReadJson(JsonReader reader, Type objectType, FPackedNormal existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FPackedRGBA16NConverter : JsonConverter<FPackedRGBA16N>
{
    public override void WriteJson(JsonWriter writer, FPackedRGBA16N value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("X");
        writer.WriteValue(value.X);

        writer.WritePropertyName("Y");
        writer.WriteValue(value.Y);

        writer.WritePropertyName("Z");
        writer.WriteValue(value.Z);

        writer.WritePropertyName("W");
        writer.WriteValue(value.X);

        writer.WriteEndObject();
    }

    public override FPackedRGBA16N ReadJson(JsonReader reader, Type objectType, FPackedRGBA16N existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FWorldConditionQueryDefinitionConverter : JsonConverter<FWorldConditionQueryDefinition>
{
    public override void WriteJson(JsonWriter writer, FWorldConditionQueryDefinition value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("StaticStruct");
        serializer.Serialize(writer, value.StaticStruct);

        writer.WritePropertyName("SharedDefinition");
        serializer.Serialize(writer, value.SharedDefinition);

        writer.WriteEndObject();
    }

    public override FWorldConditionQueryDefinition ReadJson(JsonReader reader, Type objectType, FWorldConditionQueryDefinition existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class WwiseConverter : JsonConverter<WwiseReader>
{
    public override void WriteJson(JsonWriter writer, WwiseReader value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Header");
        serializer.Serialize(writer, value.Header);

        writer.WritePropertyName("Folders");
        serializer.Serialize(writer, value.Folders);

        writer.WritePropertyName("Initialization");
        serializer.Serialize(writer, value.Initialization);

        writer.WritePropertyName("WemIndexes");
        serializer.Serialize(writer, value.WemIndexes);

        writer.WritePropertyName("Hierarchies");
        serializer.Serialize(writer, value.Hierarchies);

        writer.WritePropertyName("IdToString");
        serializer.Serialize(writer, value.IdToString);

        writer.WritePropertyName("Platform");
        writer.WriteValue(value.Platform);

        writer.WriteEndObject();
    }

    public override WwiseReader ReadJson(JsonReader reader, Type objectType, WwiseReader existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FReferenceSkeletonConverter : JsonConverter<FReferenceSkeleton>
{
    public override void WriteJson(JsonWriter writer, FReferenceSkeleton value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("FinalRefBoneInfo");
        writer.WriteStartArray();
        {
            foreach (var boneInfo in value.FinalRefBoneInfo)
            {
                serializer.Serialize(writer, boneInfo);
            }
        }
        writer.WriteEndArray();

        writer.WritePropertyName("FinalRefBonePose");
        writer.WriteStartArray();
        {
            foreach (var bonePose in value.FinalRefBonePose)
            {
                serializer.Serialize(writer, bonePose);
            }
        }
        writer.WriteEndArray();

        writer.WritePropertyName("FinalNameToIndexMap");
        serializer.Serialize(writer, value.FinalNameToIndexMap);

        writer.WriteEndObject();
    }

    public override FReferenceSkeleton ReadJson(JsonReader reader, Type objectType, FReferenceSkeleton existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FFormatContainerConverter : JsonConverter<FFormatContainer>
{
    public override void WriteJson(JsonWriter writer, FFormatContainer value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        foreach (var kvp in value.Formats)
        {
            writer.WritePropertyName(kvp.Key.Text);
            serializer.Serialize(writer, kvp.Value);
        }

        writer.WriteEndObject();
    }

    public override FFormatContainer ReadJson(JsonReader reader, Type objectType, FFormatContainer existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSmartNameMappingConverter : JsonConverter<FSmartNameMapping>
{
    public override void WriteJson(JsonWriter writer, FSmartNameMapping value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("GuidMap");
        serializer.Serialize(writer, value.GuidMap);

        writer.WritePropertyName("UidMap");
        serializer.Serialize(writer, value.UidMap);

        writer.WritePropertyName("CurveMetaDataMap");
        serializer.Serialize(writer, value.CurveMetaDataMap);

        writer.WriteEndObject();
    }

    public override FSmartNameMapping ReadJson(JsonReader reader, Type objectType, FSmartNameMapping existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FReflectionCaptureDataConverter : JsonConverter<FReflectionCaptureData>
{
    public override void WriteJson(JsonWriter writer, FReflectionCaptureData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("CubemapSize");
        writer.WriteValue(value.CubemapSize);

        writer.WritePropertyName("AverageBrightness");
        writer.WriteValue(value.AverageBrightness);

        writer.WritePropertyName("Brightness");
        writer.WriteValue(value.Brightness);

        if (value.EncodedCaptureData != null)
        {
            writer.WritePropertyName("EncodedCaptureData");
            serializer.Serialize(writer, value.EncodedCaptureData);
        }

        writer.WriteEndObject();
    }

    public override FReflectionCaptureData ReadJson(JsonReader reader, Type objectType, FReflectionCaptureData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStaticMeshComponentLODInfoConverter : JsonConverter<FStaticMeshComponentLODInfo>
{
    public override void WriteJson(JsonWriter writer, FStaticMeshComponentLODInfo value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("MapBuildDataId");
        writer.WriteValue(value.MapBuildDataId.ToString());

        if (value.OverrideVertexColors != null)
        {
            writer.WritePropertyName("OverrideVertexColors");
            serializer.Serialize(writer, value.OverrideVertexColors);
        }

        writer.WriteEndObject();
    }

    public override FStaticMeshComponentLODInfo ReadJson(JsonReader reader, Type objectType, FStaticMeshComponentLODInfo existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FMeshMapBuildDataConverter : JsonConverter<FMeshMapBuildData>
{
    public override void WriteJson(JsonWriter writer, FMeshMapBuildData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (value.LightMap != null)
        {
            writer.WritePropertyName("LightMap");
            serializer.Serialize(writer, value.LightMap);
        }

        if (value.ShadowMap != null)
        {
            writer.WritePropertyName("ShadowMap");
            serializer.Serialize(writer, value.ShadowMap);
        }

        writer.WritePropertyName("IrrelevantLights");
        serializer.Serialize(writer, value.IrrelevantLights);

        writer.WritePropertyName("PerInstanceLightmapData");
        serializer.Serialize(writer, value.PerInstanceLightmapData);

        writer.WriteEndObject();
    }

    public override FMeshMapBuildData ReadJson(JsonReader reader, Type objectType, FMeshMapBuildData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FLightMap2DConverter : JsonConverter<FLightMap2D>
{
    public override void WriteJson(JsonWriter writer, FLightMap2D value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Textures");
        serializer.Serialize(writer, value.Textures);

        if (!value.SkyOcclusionTexture?.IsNull ?? false)
        {
            writer.WritePropertyName("SkyOcclusionTexture");
            serializer.Serialize(writer, value.SkyOcclusionTexture);
        }

        if (!value.AOMaterialMaskTexture?.IsNull ?? false)
        {
            writer.WritePropertyName("AOMaterialMaskTexture");
            serializer.Serialize(writer, value.AOMaterialMaskTexture);
        }

        if (!value.ShadowMapTexture?.IsNull ?? false)
        {
            writer.WritePropertyName("ShadowMapTexture");
            serializer.Serialize(writer, value.ShadowMapTexture);
        }

        writer.WritePropertyName("VirtualTextures");
        serializer.Serialize(writer, value.VirtualTextures);

        writer.WritePropertyName("ScaleVectors");
        serializer.Serialize(writer, value.ScaleVectors);

        writer.WritePropertyName("AddVectors");
        serializer.Serialize(writer, value.AddVectors);

        writer.WritePropertyName("CoordinateScale");
        serializer.Serialize(writer, value.CoordinateScale);

        writer.WritePropertyName("CoordinateBias");
        serializer.Serialize(writer, value.CoordinateBias);

        writer.WritePropertyName("InvUniformPenumbraSize");
        serializer.Serialize(writer, value.InvUniformPenumbraSize);

        writer.WritePropertyName("bShadowChannelValid");
        serializer.Serialize(writer, value.bShadowChannelValid);

        /*
         * FLightMap
         */
        writer.WritePropertyName("LightGuids");
        serializer.Serialize(writer, value.LightGuids);

        writer.WriteEndObject();
    }

    public override FLightMap2D ReadJson(JsonReader reader, Type objectType, FLightMap2D existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FFontDataConverter : JsonConverter<FFontData>
{
    public override void WriteJson(JsonWriter writer, FFontData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (value.LocalFontFaceAsset != null)
        {
            writer.WritePropertyName("LocalFontFaceAsset");
            serializer.Serialize(writer, value.LocalFontFaceAsset);
        }
        else
        {
            if (!string.IsNullOrEmpty(value.FontFilename))
            {
                writer.WritePropertyName("FontFilename");
                writer.WriteValue(value.FontFilename);
            }

            writer.WritePropertyName("Hinting");
            writer.WriteValue(value.Hinting);

            writer.WritePropertyName("LoadingPolicy");
            writer.WriteValue(value.LoadingPolicy);
        }

        writer.WritePropertyName("SubFaceIndex");
        writer.WriteValue(value.SubFaceIndex);

        writer.WriteEndObject();
    }

    public override FFontData ReadJson(JsonReader reader, Type objectType, FFontData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStreamedAudioChunkConverter : JsonConverter<FStreamedAudioChunk>
{
    public override void WriteJson(JsonWriter writer, FStreamedAudioChunk value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("DataSize");
        writer.WriteValue(value.DataSize);

        writer.WritePropertyName("AudioDataSize");
        writer.WriteValue(value.AudioDataSize);

        writer.WritePropertyName("SeekOffsetInAudioFrames");
        writer.WriteValue(value.SeekOffsetInAudioFrames);

        writer.WritePropertyName("BulkData");
        serializer.Serialize(writer, value.BulkData);

        writer.WriteEndObject();
    }

    public override FStreamedAudioChunk ReadJson(JsonReader reader, Type objectType, FStreamedAudioChunk existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStreamedAudioPlatformDataConverter : JsonConverter<FStreamedAudioPlatformData>
{
    public override void WriteJson(JsonWriter writer, FStreamedAudioPlatformData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("NumChunks");
        writer.WriteValue(value.NumChunks);

        writer.WritePropertyName("AudioFormat");
        serializer.Serialize(writer, value.AudioFormat);

        writer.WritePropertyName("Chunks");
        serializer.Serialize(writer, value.Chunks);

        writer.WriteEndObject();
    }

    public override FStreamedAudioPlatformData ReadJson(JsonReader reader, Type objectType, FStreamedAudioPlatformData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FTexture2DMipMapConverter : JsonConverter<FTexture2DMipMap>
{
    public override void WriteJson(JsonWriter writer, FTexture2DMipMap value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("BulkData");
        serializer.Serialize(writer, value.BulkData);

        writer.WritePropertyName("SizeX");
        writer.WriteValue(value.SizeX);

        writer.WritePropertyName("SizeY");
        writer.WriteValue(value.SizeY);

        writer.WritePropertyName("SizeZ");
        writer.WriteValue(value.SizeZ);

        writer.WriteEndObject();
    }

    public override FTexture2DMipMap ReadJson(JsonReader reader, Type objectType, FTexture2DMipMap existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSkeletalMeshVertexBufferConverter : JsonConverter<FSkeletalMeshVertexBuffer>
{
    public override void WriteJson(JsonWriter writer, FSkeletalMeshVertexBuffer value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("NumTexCoords");
        writer.WriteValue(value.NumTexCoords);

        writer.WritePropertyName("MeshExtension");
        serializer.Serialize(writer, value.MeshExtension);

        writer.WritePropertyName("MeshOrigin");
        serializer.Serialize(writer, value.MeshOrigin);

        writer.WritePropertyName("bUseFullPrecisionUVs");
        writer.WriteValue(value.bUseFullPrecisionUVs);

        writer.WritePropertyName("bExtraBoneInfluences");
        writer.WriteValue(value.bExtraBoneInfluences);

        writer.WriteEndObject();
    }

    public override FSkeletalMeshVertexBuffer ReadJson(JsonReader reader, Type objectType, FSkeletalMeshVertexBuffer existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSkeletalMaterialConverter : JsonConverter<FSkeletalMaterial>
{
    public override void WriteJson(JsonWriter writer, FSkeletalMaterial value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("MaterialSlotName");
        serializer.Serialize(writer, value.MaterialSlotName);

        writer.WritePropertyName("Material");
        serializer.Serialize(writer, value.Material);

        writer.WritePropertyName("ImportedMaterialSlotName");
        serializer.Serialize(writer, value.ImportedMaterialSlotName);

        writer.WritePropertyName("UVChannelData");
        serializer.Serialize(writer, value.UVChannelData);

        writer.WriteEndObject();
    }

    public override FSkeletalMaterial ReadJson(JsonReader reader, Type objectType, FSkeletalMaterial existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSkeletalMeshVertexColorBufferConverter : JsonConverter<FSkeletalMeshVertexColorBuffer>
{
    public override void WriteJson(JsonWriter writer, FSkeletalMeshVertexColorBuffer value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Data);
    }

    public override FSkeletalMeshVertexColorBuffer ReadJson(JsonReader reader, Type objectType, FSkeletalMeshVertexColorBuffer existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSkelMeshChunkConverter : JsonConverter<FSkelMeshChunk>
{
    public override void WriteJson(JsonWriter writer, FSkelMeshChunk value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("BaseVertexIndex");
        writer.WriteValue(value.BaseVertexIndex);

        writer.WritePropertyName("NumRigidVertices");
        writer.WriteValue(value.NumRigidVertices);

        writer.WritePropertyName("NumSoftVertices");
        writer.WriteValue(value.NumSoftVertices);

        writer.WritePropertyName("MaxBoneInfluences");
        writer.WriteValue(value.MaxBoneInfluences);

        writer.WritePropertyName("HasClothData");
        writer.WriteValue(value.HasClothData);

        writer.WriteEndObject();
    }

    public override FSkelMeshChunk ReadJson(JsonReader reader, Type objectType, FSkelMeshChunk existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSkelMeshSectionConverter : JsonConverter<FSkelMeshSection>
{
    public override void WriteJson(JsonWriter writer, FSkelMeshSection value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("MaterialIndex");
        writer.WriteValue(value.MaterialIndex);

        writer.WritePropertyName("BaseIndex");
        writer.WriteValue(value.BaseIndex);

        writer.WritePropertyName("NumTriangles");
        writer.WriteValue(value.NumTriangles);

        writer.WritePropertyName("bRecomputeTangent");
        writer.WriteValue(value.bRecomputeTangent);

        writer.WritePropertyName("RecomputeTangentsVertexMaskChannel");
        writer.WriteValue(value.RecomputeTangentsVertexMaskChannel.ToString());

        writer.WritePropertyName("bCastShadow");
        writer.WriteValue(value.bCastShadow);

        writer.WritePropertyName("bVisibleInRayTracing");
        writer.WriteValue(value.bVisibleInRayTracing);

        writer.WritePropertyName("bLegacyClothingSection");
        writer.WriteValue(value.bLegacyClothingSection);

        writer.WritePropertyName("CorrespondClothSectionIndex");
        writer.WriteValue(value.CorrespondClothSectionIndex);

        writer.WritePropertyName("BaseVertexIndex");
        writer.WriteValue(value.BaseVertexIndex);

        //writer.WritePropertyName("SoftVertices");
        //serializer.Serialize(writer, value.SoftVertices);

        //writer.WritePropertyName("ClothMappingDataLODs");
        //serializer.Serialize(writer, value.ClothMappingDataLODs);

        //writer.WritePropertyName("BoneMap");
        //serializer.Serialize(writer, value.BoneMap);

        writer.WritePropertyName("NumVertices");
        writer.WriteValue(value.NumVertices);

        writer.WritePropertyName("MaxBoneInfluences");
        writer.WriteValue(value.MaxBoneInfluences);

        writer.WritePropertyName("bUse16BitBoneIndex");
        writer.WriteValue(value.bUse16BitBoneIndex);

        writer.WritePropertyName("CorrespondClothAssetIndex");
        writer.WriteValue(value.CorrespondClothAssetIndex);

        //writer.WritePropertyName("ClothingData");
        //serializer.Serialize(writer, value.ClothingData);

        //writer.WritePropertyName("OverlappingVertices");
        //serializer.Serialize(writer, value.OverlappingVertices);

        writer.WritePropertyName("bDisabled");
        writer.WriteValue(value.bDisabled);

        writer.WritePropertyName("GenerateUpToLodIndex");
        writer.WriteValue(value.GenerateUpToLodIndex);

        writer.WritePropertyName("OriginalDataSectionIndex");
        writer.WriteValue(value.OriginalDataSectionIndex);

        writer.WritePropertyName("ChunkedParentSectionIndex");
        writer.WriteValue(value.ChunkedParentSectionIndex);

        writer.WriteEndObject();
    }

    public override FSkelMeshSection ReadJson(JsonReader reader, Type objectType, FSkelMeshSection existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSkelMeshVertexBaseConverter : JsonConverter<FSkelMeshVertexBase>
{
    public override void WriteJson(JsonWriter writer, FSkelMeshVertexBase value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (!value.Pos.IsZero())
        {
            writer.WritePropertyName("Pos");
            serializer.Serialize(writer, value.Pos);
        }

        if (value.Normal.Length > 0)
        {
            writer.WritePropertyName("Normal");
            serializer.Serialize(writer, value.Normal);
        }

        if (value.Infs != null)
        {
            writer.WritePropertyName("Infs");
            serializer.Serialize(writer, value.Infs);
        }

        writer.WriteEndObject();
    }

    public override FSkelMeshVertexBase ReadJson(JsonReader reader, Type objectType, FSkelMeshVertexBase existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStaticMeshLODResourcesConverter : JsonConverter<FStaticMeshLODResources>
{
    public override void WriteJson(JsonWriter writer, FStaticMeshLODResources value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Sections");
        serializer.Serialize(writer, value.Sections);

        writer.WritePropertyName("MaxDeviation");
        writer.WriteValue(value.MaxDeviation);

        writer.WritePropertyName("PositionVertexBuffer");
        serializer.Serialize(writer, value.PositionVertexBuffer);

        writer.WritePropertyName("VertexBuffer");
        serializer.Serialize(writer, value.VertexBuffer);

        writer.WritePropertyName("ColorVertexBuffer");
        serializer.Serialize(writer, value.ColorVertexBuffer);

        if (value.CardRepresentationData != null)
        {
            writer.WritePropertyName("CardRepresentationData");
            serializer.Serialize(writer, value.CardRepresentationData);
        }

        writer.WriteEndObject();
    }

    public override FStaticMeshLODResources ReadJson(JsonReader reader, Type objectType, FStaticMeshLODResources existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FMaterialParameterInfoConverter : JsonConverter<FMaterialParameterInfo>
{
    public override void WriteJson(JsonWriter writer, FMaterialParameterInfo value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Name");
        serializer.Serialize(writer, value.Name);

        writer.WritePropertyName("Association");
        writer.WriteValue($"EMaterialParameterAssociation::{value.Association.ToString()}");

        writer.WritePropertyName("Index");
        writer.WriteValue(value.Index);

        writer.WriteEndObject();
    }

    public override FMaterialParameterInfo ReadJson(JsonReader reader, Type objectType, FMaterialParameterInfo existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FVirtualTextureDataChunkConverter : JsonConverter<FVirtualTextureDataChunk>
{
    public override void WriteJson(JsonWriter writer, FVirtualTextureDataChunk value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("BulkData");
        serializer.Serialize(writer, value.BulkData);

        writer.WritePropertyName("SizeInBytes");
        writer.WriteValue(value.SizeInBytes);

        writer.WritePropertyName("CodecPayloadSize");
        writer.WriteValue(value.CodecPayloadSize);

        writer.WritePropertyName("CodecPayloadOffset");
        serializer.Serialize(writer, value.CodecPayloadOffset);

        writer.WritePropertyName("CodecType");
        writer.WriteStartArray();
        foreach (var codec in value.CodecType)
        {
            writer.WriteValue(codec.ToString());
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public override FVirtualTextureDataChunk ReadJson(JsonReader reader, Type objectType, FVirtualTextureDataChunk existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStaticMeshRenderDataConverter : JsonConverter<FStaticMeshRenderData>
{
    public override void WriteJson(JsonWriter writer, FStaticMeshRenderData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("LODs");
        serializer.Serialize(writer, value.LODs);

        if (value.NaniteResources != null)
        {
            writer.WritePropertyName("NaniteResources");
            serializer.Serialize(writer, value.NaniteResources);
        }

        writer.WritePropertyName("Bounds");
        serializer.Serialize(writer, value.Bounds);

        writer.WritePropertyName("bLODsShareStaticLighting");
        writer.WriteValue(value.bLODsShareStaticLighting);

        writer.WritePropertyName("ScreenSize");
        serializer.Serialize(writer, value.ScreenSize);

        writer.WriteEndObject();
    }

    public override FStaticMeshRenderData ReadJson(JsonReader reader, Type objectType, FStaticMeshRenderData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStaticMeshSectionConverter : JsonConverter<FStaticMeshSection>
{
    public override void WriteJson(JsonWriter writer, FStaticMeshSection value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("MaterialIndex");
        writer.WriteValue(value.MaterialIndex);

        writer.WritePropertyName("FirstIndex");
        writer.WriteValue(value.FirstIndex);

        writer.WritePropertyName("NumTriangles");
        writer.WriteValue(value.NumTriangles);

        writer.WritePropertyName("MinVertexIndex");
        writer.WriteValue(value.MinVertexIndex);

        writer.WritePropertyName("MaxVertexIndex");
        writer.WriteValue(value.MaxVertexIndex);

        writer.WritePropertyName("bEnableCollision");
        writer.WriteValue(value.bEnableCollision);

        writer.WritePropertyName("bCastShadow");
        writer.WriteValue(value.bCastShadow);

        writer.WritePropertyName("bForceOpaque");
        writer.WriteValue(value.bForceOpaque);

        writer.WritePropertyName("bVisibleInRayTracing");
        writer.WriteValue(value.bVisibleInRayTracing);

        writer.WriteEndObject();
    }

    public override FStaticMeshSection ReadJson(JsonReader reader, Type objectType, FStaticMeshSection existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStaticMeshUVItemConverter : JsonConverter<FStaticMeshUVItem>
{
    public override void WriteJson(JsonWriter writer, FStaticMeshUVItem value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Normal");
        serializer.Serialize(writer, value.Normal);

        writer.WritePropertyName("UV");
        serializer.Serialize(writer, value.UV);

        writer.WriteEndObject();
    }

    public override FStaticMeshUVItem ReadJson(JsonReader reader, Type objectType, FStaticMeshUVItem existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStaticMeshVertexBufferConverter : JsonConverter<FStaticMeshVertexBuffer>
{
    public override void WriteJson(JsonWriter writer, FStaticMeshVertexBuffer value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("NumTexCoords");
        writer.WriteValue(value.NumTexCoords);

        writer.WritePropertyName("NumVertices");
        writer.WriteValue(value.NumVertices);

        writer.WritePropertyName("Strides");
        writer.WriteValue(value.Strides);

        writer.WritePropertyName("UseHighPrecisionTangentBasis");
        writer.WriteValue(value.UseHighPrecisionTangentBasis);

        writer.WritePropertyName("UseFullPrecisionUVs");
        writer.WriteValue(value.UseFullPrecisionUVs);

        writer.WriteEndObject();
    }

    public override FStaticMeshVertexBuffer ReadJson(JsonReader reader, Type objectType, FStaticMeshVertexBuffer existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FStaticLODModelConverter : JsonConverter<FStaticLODModel>
{
    public override void WriteJson(JsonWriter writer, FStaticLODModel value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Sections");
        serializer.Serialize(writer, value.Sections);

        // writer.WritePropertyName("Indices");
        // serializer.Serialize(writer, value.Indices);

        // writer.WritePropertyName("ActiveBoneIndices");
        // serializer.Serialize(writer, value.ActiveBoneIndices);

        writer.WritePropertyName("NumVertices");
        writer.WriteValue(value.NumVertices);

        writer.WritePropertyName("NumTexCoords");
        writer.WriteValue(value.NumTexCoords);

        // writer.WritePropertyName("RequiredBones");
        // serializer.Serialize(writer, value.RequiredBones);

        if (value.MorphTargetVertexInfoBuffers != null)
        {
            writer.WritePropertyName("MorphTargetVertexInfoBuffers");
            serializer.Serialize(writer, value.MorphTargetVertexInfoBuffers);
        }

        if (value.VertexAttributeBuffers != null)
        {
            writer.WritePropertyName("VertexAttributeBuffers");
            serializer.Serialize(writer, value.VertexAttributeBuffers);
        }

        writer.WritePropertyName("VertexBufferGPUSkin");
        serializer.Serialize(writer, value.VertexBufferGPUSkin);

        // writer.WritePropertyName("ColorVertexBuffer");
        // serializer.Serialize(writer, value.ColorVertexBuffer);

        // writer.WritePropertyName("AdjacencyIndexBuffer");
        // serializer.Serialize(writer, value.AdjacencyIndexBuffer);

        if (value.Chunks.Length > 0)
        {
            writer.WritePropertyName("Chunks");
            serializer.Serialize(writer, value.Chunks);

            // writer.WritePropertyName("ClothVertexBuffer");
            // serializer.Serialize(writer, value.ClothVertexBuffer);
        }

        if (value.MeshToImportVertexMap.Length > 0)
        {
            // writer.WritePropertyName("MeshToImportVertexMap");
            // serializer.Serialize(writer, value.MeshToImportVertexMap);

            writer.WritePropertyName("MaxImportVertex");
            serializer.Serialize(writer, value.MaxImportVertex);
        }

        writer.WriteEndObject();
    }

    public override FStaticLODModel ReadJson(JsonReader reader, Type objectType, FStaticLODModel existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FDictionaryHeaderConverter : JsonConverter<FDictionaryHeader>
{
    public override void WriteJson(JsonWriter writer, FDictionaryHeader value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Magic");
        serializer.Serialize(writer, value.Magic);

        writer.WritePropertyName("DictionaryVersion");
        serializer.Serialize(writer, value.DictionaryVersion);

        writer.WritePropertyName("OodleMajorHeaderVersion");
        serializer.Serialize(writer, value.OodleMajorHeaderVersion);

        writer.WritePropertyName("HashTableSize");
        serializer.Serialize(writer, value.HashTableSize);

        writer.WritePropertyName("DictionaryData");
        serializer.Serialize(writer, value.DictionaryData);

        writer.WritePropertyName("CompressorData");
        serializer.Serialize(writer, value.CompressorData);

        writer.WriteEndObject();
    }

    public override FDictionaryHeader ReadJson(JsonReader reader, Type objectType, FDictionaryHeader existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FInstancedStructConverter : JsonConverter<FInstancedStruct>
{
    public override void WriteJson(JsonWriter writer, FInstancedStruct? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value?.NonConstStruct);
    }

    public override FInstancedStruct ReadJson(JsonReader reader, Type objectType, FInstancedStruct? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FACLCompressedAnimDataConverter : JsonConverter<FACLCompressedAnimData>
{
    public override void WriteJson(JsonWriter writer, FACLCompressedAnimData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("CompressedNumberOfFrames");
        writer.WriteValue(value.CompressedNumberOfFrames);

        /*writer.WritePropertyName("CompressedByteStream");
        writer.WriteValue(value.CompressedByteStream);*/

        writer.WriteEndObject();
    }

    public override FACLCompressedAnimData ReadJson(JsonReader reader, Type objectType, FACLCompressedAnimData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class HierarchyConverter : JsonConverter<Hierarchy>
{
    public override void WriteJson(JsonWriter writer, Hierarchy value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Type");
        writer.WriteValue(value.Type.ToString());

        writer.WritePropertyName("Length");
        writer.WriteValue(value.Length);

        writer.WritePropertyName("Data");
        serializer.Serialize(writer, value.Data);

        writer.WriteEndObject();
    }

    public override Hierarchy ReadJson(JsonReader reader, Type objectType, Hierarchy existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FByteBulkDataHeaderConverter : JsonConverter<FByteBulkDataHeader>
{
    public override void WriteJson(JsonWriter writer, FByteBulkDataHeader value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("BulkDataFlags");
        writer.WriteValue(value.BulkDataFlags.ToStringBitfield());

        writer.WritePropertyName("ElementCount");
        writer.WriteValue(value.ElementCount);

        writer.WritePropertyName("SizeOnDisk");
        writer.WriteValue(value.SizeOnDisk);

        writer.WritePropertyName("OffsetInFile");
        writer.WriteValue($"0x{value.OffsetInFile:X}");

        writer.WriteEndObject();
    }

    public override FByteBulkDataHeader ReadJson(JsonReader reader, Type objectType, FByteBulkDataHeader existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FByteBulkDataConverter : JsonConverter<FByteBulkData>
{
    public override void WriteJson(JsonWriter writer, FByteBulkData value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Header);
    }

    public override FByteBulkData ReadJson(JsonReader reader, Type objectType, FByteBulkData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FKismetPropertyPointerConverter : JsonConverter<FKismetPropertyPointer>
{
    public override FKismetPropertyPointer? ReadJson(JsonReader reader, Type objectType, FKismetPropertyPointer? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FKismetPropertyPointer value, JsonSerializer serializer)
    {
        if (value.bNew)
        {
            value.New!.WriteJson(writer, serializer);
        }
        else
        {
            value.Old!.WriteJson(writer, serializer);
        }
    }
}

public class KismetExpressionConverter : JsonConverter<KismetExpression>
{
    public override void WriteJson(JsonWriter writer, KismetExpression value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        value.WriteJson(writer, serializer);
        writer.WriteEndObject();
    }

    public override KismetExpression ReadJson(JsonReader reader, Type objectType, KismetExpression existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FCurveDescConverter : JsonConverter<FCurveDesc>
{
    public override void WriteJson(JsonWriter writer, FCurveDesc value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("CompressionFormat");
        writer.WriteValue(value.CompressionFormat.ToString());

        writer.WritePropertyName("KeyTimeCompressionFormat");
        writer.WriteValue(value.KeyTimeCompressionFormat.ToString());

        writer.WritePropertyName("PreInfinityExtrap");
        writer.WriteValue(value.PreInfinityExtrap.ToString());

        writer.WritePropertyName("PostInfinityExtrap");
        writer.WriteValue(value.PostInfinityExtrap.ToString());

        if (value.CompressionFormat == ERichCurveCompressionFormat.RCCF_Constant)
        {
            writer.WritePropertyName("ConstantValue");
            writer.WriteValue(value.ConstantValue);
        }
        else
        {
            writer.WritePropertyName("NumKeys");
            writer.WriteValue(value.NumKeys);
        }

        writer.WritePropertyName("KeyDataOffset");
        writer.WriteValue(value.KeyDataOffset);

        writer.WriteEndObject();
    }

    public override FCurveDesc ReadJson(JsonReader reader, Type objectType, FCurveDesc existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FMeshBoneInfoConverter : JsonConverter<FMeshBoneInfo>
{
    public override void WriteJson(JsonWriter writer, FMeshBoneInfo value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Name");
        serializer.Serialize(writer, value.Name);

        writer.WritePropertyName("ParentIndex");
        writer.WriteValue(value.ParentIndex);

        writer.WriteEndObject();
    }

    public override FMeshBoneInfo ReadJson(JsonReader reader, Type objectType, FMeshBoneInfo existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FReferencePoseConverter : JsonConverter<FReferencePose>
{
    public override void WriteJson(JsonWriter writer, FReferencePose value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("PoseName");
        serializer.Serialize(writer, value.PoseName);

        writer.WritePropertyName("ReferencePose");
        writer.WriteStartArray();
        {
            foreach (var pose in value.ReferencePose)
            {
                serializer.Serialize(writer, pose);
            }
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public override FReferencePose ReadJson(JsonReader reader, Type objectType, FReferencePose existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FTextLocalizationMetaDataResourceConverter : JsonConverter<FTextLocalizationMetaDataResource>
{
    public override void WriteJson(JsonWriter writer, FTextLocalizationMetaDataResource value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("NativeCulture");
        writer.WriteValue(value.NativeCulture);

        writer.WritePropertyName("NativeLocRes");
        writer.WriteValue(value.NativeLocRes);

        writer.WritePropertyName("CompiledCultures");
        serializer.Serialize(writer, value.CompiledCultures);

        writer.WriteEndObject();
    }

    public override FTextLocalizationMetaDataResource ReadJson(JsonReader reader, Type objectType, FTextLocalizationMetaDataResource existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FEndTextResourceStringsConverter : JsonConverter<FEndTextResourceStrings>
{
    public override void WriteJson(JsonWriter writer, FEndTextResourceStrings value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (value.Entries?.Count > 0)
        {
            writer.WritePropertyName("Entries");
            serializer.Serialize(writer, value.Entries);
        }

        writer.WriteEndObject();
    }

    public override FEndTextResourceStrings ReadJson(JsonReader reader, Type objectType, FEndTextResourceStrings? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FAssetPackageDataConverter : JsonConverter<FAssetPackageData>
{
    public override void WriteJson(JsonWriter writer, FAssetPackageData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("PackageName");
        serializer.Serialize(writer, value.PackageName);

        writer.WritePropertyName("DiskSize");
        serializer.Serialize(writer, value.DiskSize);

        writer.WritePropertyName("PackageGuid");
        serializer.Serialize(writer, value.PackageGuid);

        if (value.CookedHash != null)
        {
            writer.WritePropertyName("CookedHash");
            serializer.Serialize(writer, value.CookedHash);
        }

        if (value.FileVersionUE.FileVersionUE4 != 0 || value.FileVersionUE.FileVersionUE5 != 0)
        {
            writer.WritePropertyName("FileVersionUE");
            serializer.Serialize(writer, value.FileVersionUE);
        }

        if (value.FileVersionLicenseeUE != -1)
        {
            writer.WritePropertyName("FileVersionLicenseeUE");
            serializer.Serialize(writer, value.FileVersionLicenseeUE);
        }

        if (value.Flags != 0)
        {
            writer.WritePropertyName("Flags");
            serializer.Serialize(writer, value.Flags);
        }

        if (value.CustomVersions?.Versions is { Length: > 0 })
        {
            writer.WritePropertyName("CustomVersions");
            serializer.Serialize(writer, value.CustomVersions);
        }

        if (value.ImportedClasses is { Length: > 0 })
        {
            writer.WritePropertyName("ImportedClasses");
            serializer.Serialize(writer, value.ImportedClasses);
        }

        writer.WriteEndObject();
    }

    public override FAssetPackageData ReadJson(JsonReader reader, Type objectType, FAssetPackageData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FConnectivityCubeConverter : JsonConverter<FConnectivityCube>
{
    public override void WriteJson(JsonWriter writer, FConnectivityCube value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        for (int i = 0; i < value.Faces.Length; i++)
        {
            var face = value.Faces[i];
            writer.WritePropertyName(((EFortConnectivityCubeFace) i).ToString());
            writer.WriteStartArray();
            for (int j = 0; j < face.Length; j++)
            {
                writer.WriteValue(face[j]);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    public override FConnectivityCube ReadJson(JsonReader reader, Type objectType, FConnectivityCube existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FDependsNodeConverter : JsonConverter<FDependsNode>
{
    public override void WriteJson(JsonWriter writer, FDependsNode value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Identifier");
        serializer.Serialize(writer, value.Identifier);

        WriteDependsNodeList("PackageDependencies", writer, value.PackageDependencies);
        WriteDependsNodeList("NameDependencies", writer, value.NameDependencies);
        WriteDependsNodeList("ManageDependencies", writer, value.ManageDependencies);
        WriteDependsNodeList("Referencers", writer, value.Referencers);

        if (value.PackageFlags != null)
        {
            writer.WritePropertyName("PackageFlags");
            serializer.Serialize(writer, value.PackageFlags);
        }

        if (value.ManageFlags != null)
        {
            writer.WritePropertyName("ManageFlags");
            serializer.Serialize(writer, value.ManageFlags);
        }

        writer.WriteEndObject();
    }

    /** Custom serializer to avoid circular reference */
    private static void WriteDependsNodeList(string name, JsonWriter writer, List<FDependsNode>? dependsNodeList)
    {
        if (dependsNodeList == null || dependsNodeList.Count == 0)
        {
            return;
        }

        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var dependsNode in dependsNodeList)
        {
            writer.WriteValue(dependsNode._index);
        }
        writer.WriteEndArray();
    }

    public override FDependsNode ReadJson(JsonReader reader, Type objectType, FDependsNode existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FTopLevelAssetPathConverter : JsonConverter<FTopLevelAssetPath>
{
    public override void WriteJson(JsonWriter writer, FTopLevelAssetPath value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override FTopLevelAssetPath ReadJson(JsonReader reader, Type objectType, FTopLevelAssetPath existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FAssetDataConverter : JsonConverter<FAssetData>
{
    public override void WriteJson(JsonWriter writer, FAssetData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("ObjectPath");
        serializer.Serialize(writer, value.ObjectPath);

        writer.WritePropertyName("PackageName");
        serializer.Serialize(writer, value.PackageName);

        writer.WritePropertyName("PackagePath");
        serializer.Serialize(writer, value.PackagePath);

        writer.WritePropertyName("AssetName");
        serializer.Serialize(writer, value.AssetName);

        writer.WritePropertyName("AssetClass");
        serializer.Serialize(writer, value.AssetClass);

        if (value.TagsAndValues.Count > 0)
        {
            writer.WritePropertyName("TagsAndValues");
            serializer.Serialize(writer, value.TagsAndValues);
        }

        if (value.TaggedAssetBundles.Bundles.Length > 0)
        {
            writer.WritePropertyName("TaggedAssetBundles");
            serializer.Serialize(writer, value.TaggedAssetBundles);
        }

        if (value.ChunkIDs.Length > 0)
        {
            writer.WritePropertyName("ChunkIDs");
            serializer.Serialize(writer, value.ChunkIDs);
        }

        if (value.PackageFlags != 0)
        {
            writer.WritePropertyName("PackageFlags");
            serializer.Serialize(writer, value.PackageFlags);
        }

        writer.WriteEndObject();
    }

    public override FAssetData ReadJson(JsonReader reader, Type objectType, FAssetData existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSoftObjectPathConverter : JsonConverter<FSoftObjectPath>
{
    public override void WriteJson(JsonWriter writer, FSoftObjectPath value, JsonSerializer serializer)
    {
        /*var path = value.ToString();
        writer.WriteValue(path.Length > 0 ? path : "None");*/
        writer.WriteStartObject();

        writer.WritePropertyName("AssetPathName");
        serializer.Serialize(writer, value.AssetPathName);

        writer.WritePropertyName("SubPathString");
        writer.WriteValue(value.SubPathString);

        writer.WriteEndObject();
    }

    public override FSoftObjectPath ReadJson(JsonReader reader, Type objectType, FSoftObjectPath existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FTextLocalizationResourceConverter : JsonConverter<FTextLocalizationResource>
{
    public override void WriteJson(JsonWriter writer, FTextLocalizationResource value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        foreach (var nvk in value.Entries)
        {
            writer.WritePropertyName(nvk.Key.Str); // namespace
            writer.WriteStartObject();
            foreach (var kvs in nvk.Value)
            {
                writer.WritePropertyName(kvs.Key.Str); // key
                writer.WriteValue(kvs.Value.LocalizedString); // string
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    public override FTextLocalizationResource ReadJson(JsonReader reader, Type objectType, FTextLocalizationResource existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FCustomVersionContainerConverter : JsonConverter<FCustomVersionContainer>
{
    public override void WriteJson(JsonWriter writer, FCustomVersionContainer? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value?.Versions);
    }

    public override FCustomVersionContainer ReadJson(JsonReader reader, Type objectType, FCustomVersionContainer? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class ResolvedObjectConverter : JsonConverter<ResolvedObject>
{
    public override void WriteJson(JsonWriter writer, ResolvedObject value, JsonSerializer serializer)
    {
        var top = value;
        ResolvedObject outerMost;
        while (true)
        {
            var outer = top.Outer;
            if (outer == null)
            {
                outerMost = top;
                break;
            }

            top = outer;
        }

        writer.WriteStartObject();

        writer.WritePropertyName("ObjectName"); // 1:2:3 if we are talking about an export in the current asset
        writer.WriteValue(value.GetFullName(false));

        writer.WritePropertyName("ObjectPath"); // package path . export index
        var outerMostName = outerMost.Name.Text;
        writer.WriteValue(value.ExportIndex != -1 ? $"{outerMostName}.{value.ExportIndex}" : outerMostName);

        writer.WriteEndObject();
    }

    public override ResolvedObject ReadJson(JsonReader reader, Type objectType, ResolvedObject existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FNameConverter : JsonConverter<FName>
{
    public override void WriteJson(JsonWriter writer, FName value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Text);
    }

    public override FName ReadJson(JsonReader reader, Type objectType, FName existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FSmartNameConverter : JsonConverter<FSmartName>
{
    public override void WriteJson(JsonWriter writer, FSmartName value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.DisplayName);
    }

    public override FSmartName ReadJson(JsonReader reader, Type objectType, FSmartName existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FCompressedVisibilityChunkConverter : JsonConverter<FCompressedVisibilityChunk>
{
    public override void WriteJson(JsonWriter writer, FCompressedVisibilityChunk value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("bCompressed");
        writer.WriteValue(value.bCompressed);

        writer.WritePropertyName("UncompressedSize");
        writer.WriteValue(value.UncompressedSize);

        writer.WriteEndObject();
    }

    public override FCompressedVisibilityChunk ReadJson(JsonReader reader, Type objectType, FCompressedVisibilityChunk existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FGuidConverter : JsonConverter<FGuid>
{
    public override void WriteJson(JsonWriter writer, FGuid value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString(EGuidFormats.UniqueObjectGuid));
    }

    public override FGuid ReadJson(JsonReader reader, Type objectType, FGuid existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.Value is not string s)
            throw new JsonSerializationException();

        return new FGuid(s.Replace("-", ""));
    }
}

public class FAssetRegistryStateConverter : JsonConverter<FAssetRegistryState>
{
    public override void WriteJson(JsonWriter writer, FAssetRegistryState value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("PreallocatedAssetDataBuffers");
        serializer.Serialize(writer, value.PreallocatedAssetDataBuffers);

        writer.WritePropertyName("PreallocatedDependsNodeDataBuffers");
        serializer.Serialize(writer, value.PreallocatedDependsNodeDataBuffers);

        writer.WritePropertyName("PreallocatedPackageDataBuffers");
        serializer.Serialize(writer, value.PreallocatedPackageDataBuffers);

        writer.WriteEndObject();
    }

    public override FAssetRegistryState ReadJson(JsonReader reader, Type objectType, FAssetRegistryState existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FScriptTextConverter : JsonConverter<FScriptText>
{
    public override void WriteJson(JsonWriter writer, FScriptText value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        switch (value.TextLiteralType)
        {
            case EBlueprintTextLiteralType.Empty:
                writer.WritePropertyName("SourceString");
                writer.WriteValue("");
                break;
            case EBlueprintTextLiteralType.LocalizedText:
                writer.WritePropertyName("SourceString");
                serializer.Serialize(writer, value.SourceString);
                writer.WritePropertyName("KeyString");
                serializer.Serialize(writer, value.KeyString);
                writer.WritePropertyName("Namespace");
                serializer.Serialize(writer, value.Namespace);
                break;
            case EBlueprintTextLiteralType.InvariantText:
            case EBlueprintTextLiteralType.LiteralString:
                writer.WritePropertyName("SourceString");
                serializer.Serialize(writer, value.SourceString);
                break;
            case EBlueprintTextLiteralType.StringTableEntry:
                writer.WritePropertyName("StringTableAsset");
                serializer.Serialize(writer, value.StringTableAsset);
                writer.WritePropertyName("TableIdString");
                serializer.Serialize(writer, value.TableIdString);
                writer.WritePropertyName("KeyString");
                serializer.Serialize(writer, value.KeyString);
                break;
        }
        writer.WriteEndObject();
    }

    public override FScriptText? ReadJson(JsonReader reader, Type objectType, FScriptText? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class FGameplayTagConverter : JsonConverter<FGameplayTag>
{
    public override void WriteJson(JsonWriter writer, FGameplayTag value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.TagName);
    }

    public override FGameplayTag ReadJson(JsonReader reader, Type objectType, FGameplayTag existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
