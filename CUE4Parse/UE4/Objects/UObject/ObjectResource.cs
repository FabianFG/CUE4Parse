using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using UExport = CUE4Parse.UE4.Assets.Exports.UObject;

namespace CUE4Parse.UE4.Objects.UObject
{
    /// <summary>
    /// Wrapper for index into a ULinker's ImportMap or ExportMap.
    /// Values greater than zero indicate that this is an index into the ExportMap.  The
    /// actual array index will be (FPackageIndex - 1).
    ///
    /// Values less than zero indicate that this is an index into the ImportMap. The actual
    /// array index will be (-FPackageIndex - 1)
    /// </summary>
    [JsonConverter(typeof(FPackageIndexConverter))]
    public class FPackageIndex
    {
        /// <summary>
        /// Values greater than zero indicate that this is an index into the ExportMap.  The
        /// actual array index will be (FPackageIndex - 1).
        ///
        /// Values less than zero indicate that this is an index into the ImportMap. The actual
        /// array index will be (-FPackageIndex - 1)
        /// </summary>
        public readonly int Index;

        public readonly IPackage? Owner;

        public ResolvedObject? ResolvedObject => Owner?.ResolvePackageIndex(this);

        public bool IsNull => Index == 0;
        public bool IsExport => Index > 0;
        public bool IsImport => Index < 0;

        public string Name => ResolvedObject?.Name.Text ?? "None";

        public FPackageIndex(FAssetArchive Ar, int index)
        {
            Index = index;
            Owner = Ar.Owner;
        }

        public FPackageIndex(FAssetArchive Ar)
        {
            Index = Ar.Read<int>();
            Owner = Ar.Owner;
        }

        public FPackageIndex(FKismetArchive Ar)
        {
            Index = Ar.Read<int>();
            Owner = Ar.Owner;
            Ar.Index += 4;
        }

        public FPackageIndex()
        {
            Index = 0;
            Owner = null;
        }

        public override string ToString()
        {
            return ResolvedObject?.ToString() ?? Index.ToString();
        }

        protected internal void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            if (TryLoad<UProperty>(out var property))
            {
                serializer.Serialize(writer, property);
            }
            else
            {
                serializer.Serialize(writer, this);
            }
        }

        #region Loading Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Load<T>() where T : UExport => Owner?.FindObject(this)?.Value as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad<T>(out T export) where T : UExport
        {
            if (!TryLoad(out var genericExport) || genericExport is not T cast)
            {
                export = default;
                return false;
            }

            export = cast;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> LoadAsync<T>() where T : UExport =>
            await LoadAsync() as T ?? throw new ParserException($"Loaded {ToString()} but it was of wrong type");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T?> TryLoadAsync<T>() where T : UExport => await TryLoadAsync() as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport? Load() => ResolvedObject?.Load();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad(out UExport? export)
        {
            if (ResolvedObject != null)
                return ResolvedObject.TryLoad(out export);

            export = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport> LoadAsync()
        {
            if (ResolvedObject != null)
                return await ResolvedObject.LoadAsync();
            throw new ParserException($"{ToString()} could not be loaded");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport?> TryLoadAsync()
        {
            if (ResolvedObject != null)
            {
                var loadedObj = await ResolvedObject.TryLoadAsync();
                if (loadedObj != null)
                    return loadedObj;
            }

            return null;
        }

        #endregion
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


    /// <summary>
    /// Base class for UObject resource types.  FObjectResources are used to store UObjects on disk
    /// via FLinker's ImportMap (for resources contained in other packages) and ExportMap (for resources
    /// contained within the same package)
    /// </summary>
    [JsonConverter(typeof(FObjectResourceConverter))]
    public abstract class FObjectResource : IObject
    {
        public FName ObjectName;
        public FPackageIndex OuterIndex;

        public override string ToString()
        {
            return ObjectName.Text;
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

    public class FObjectExport : FObjectResource
    {
        public FPackageIndex ClassIndex;
        public FPackageIndex SuperIndex;
        public FPackageIndex TemplateIndex;
        public uint ObjectFlags;
        public long SerialSize;
        public long SerialOffset;
        public bool ForcedExport;
        public bool NotForClient;
        public bool NotForServer;
        public FGuid PackageGuid;
        public bool IsInheritedInstance;
        public uint PackageFlags;
        public bool NotAlwaysLoadedForEditorGame;
        public bool IsAsset;
        public bool GeneratePublicHash;
        public int FirstExportDependency;
        public int SerializationBeforeSerializationDependencies;
        public int CreateBeforeSerializationDependencies;
        public int SerializationBeforeCreateDependencies;
        public int CreateBeforeCreateDependencies;
        public Lazy<UExport> ExportObject;

        public string ClassName;

#pragma warning disable 8618
        public FObjectExport()
#pragma warning restore 8618
        { }

        public FObjectExport(FAssetArchive Ar)
        {
            ClassIndex = new FPackageIndex(Ar);
            SuperIndex = new FPackageIndex(Ar);
            TemplateIndex = Ar.Ver >= EUnrealEngineObjectUE4Version.TemplateIndex_IN_COOKED_EXPORTS ? new FPackageIndex(Ar) : new FPackageIndex();
            OuterIndex = new FPackageIndex(Ar);
            ObjectName = Ar.ReadFName();
            ObjectFlags = Ar.Read<uint>();

            if (Ar.Ver < EUnrealEngineObjectUE4Version.e64BIT_EXPORTMAP_SERIALSIZES)
            {
                SerialSize = Ar.Read<int>();
                SerialOffset = Ar.Read<int>();
            }
            else
            {
                SerialSize = Ar.Read<long>();
                SerialOffset = Ar.Read<long>();
            }

            ForcedExport = Ar.ReadBoolean();
            NotForClient = Ar.ReadBoolean();
            NotForServer = Ar.ReadBoolean();
            PackageGuid = Ar.Ver < EUnrealEngineObjectUE5Version.REMOVE_OBJECT_EXPORT_PACKAGE_GUID ? Ar.Read<FGuid>() : default;
            IsInheritedInstance = Ar.Ver >= EUnrealEngineObjectUE5Version.TRACK_OBJECT_EXPORT_IS_INHERITED && Ar.ReadBoolean();
            PackageFlags = Ar.Read<uint>();
            NotAlwaysLoadedForEditorGame = Ar.Ver >= EUnrealEngineObjectUE4Version.LOAD_FOR_EDITOR_GAME && Ar.ReadBoolean();
            IsAsset = Ar.Ver >= EUnrealEngineObjectUE4Version.COOKED_ASSETS_IN_EDITOR_SUPPORT && Ar.ReadBoolean();
            GeneratePublicHash = Ar.Ver >= EUnrealEngineObjectUE5Version.OPTIONAL_RESOURCES && Ar.ReadBoolean();

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS)
            {
                FirstExportDependency = Ar.Read<int>();
                SerializationBeforeSerializationDependencies = Ar.Read<int>();
                CreateBeforeSerializationDependencies = Ar.Read<int>();
                SerializationBeforeCreateDependencies = Ar.Read<int>();
                CreateBeforeCreateDependencies = Ar.Read<int>();
            }
            else
            {
                FirstExportDependency = -1;
                SerializationBeforeSerializationDependencies = 0;
                CreateBeforeSerializationDependencies = 0;
                SerializationBeforeCreateDependencies = 0;
                CreateBeforeCreateDependencies = 0;
            }

            ClassName = ClassIndex.Name;
        }

        public override string ToString()
        {
            return $"{ObjectName.Text} ({ClassIndex.Name})";
        }
    }

    /// <summary>
    /// UObject resource type for objects that are referenced by this package, but contained
    /// within another package.
    /// </summary>
    public class FObjectImport : FObjectResource
    {
        public readonly FName ClassPackage;
        public FName ClassName;
        public FName PackageName;
        public bool ImportOptional;

#pragma warning disable 8618
        public FObjectImport()
#pragma warning restore 8618
        { }

        public FObjectImport(FAssetArchive Ar)
        {
            ClassPackage = Ar.ReadFName();
            ClassName = Ar.ReadFName();
            OuterIndex = new FPackageIndex(Ar);
            ObjectName = Ar.ReadFName();

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.NON_OUTER_PACKAGE_IMPORT && !Ar.IsFilterEditorOnly)
            {
                PackageName = Ar.ReadFName();
            }
            
            ImportOptional = Ar.Ver >= EUnrealEngineObjectUE5Version.OPTIONAL_RESOURCES && Ar.ReadBoolean();
        }
    }
}
