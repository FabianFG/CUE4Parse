using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
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

        public string Name => ResolvedObject?.Name.Text ?? string.Empty;

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

        public FPackageIndex()
        {
            Index = 0;
            Owner = null;
        }

        public override string ToString()
        {
            return ResolvedObject?.ToString() ?? Index.ToString();
        }

        #region Loading Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport? Load() =>
            Load(Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad(out UExport export)
        {
            var provider = Owner?.Provider;
            if (provider == null)
            {
                export = default;
                return false;
            }

            return TryLoad(provider, out export);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Load<T>() where T : UExport => Owner?.FindObject(this)?.Value as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad<T>(out T export) where T : UExport
        {
            var provider = Owner?.Provider;
            if (provider == null)
            {
                export = default;
                return false;
            }

            return TryLoad(provider, out export);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport> LoadAsync() => await LoadAsync(Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport?> TryLoadAsync()
        {
            var provider = Owner?.Provider;
            if (provider == null) return null;
            return await TryLoadAsync(provider).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> LoadAsync<T>() where T : UExport => await LoadAsync<T>(Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T?> TryLoadAsync<T>() where T : UExport
        {
            var provider = Owner?.Provider;
            if (provider == null) return null;
            return await TryLoadAsync<T>(provider).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Load<T>(IFileProvider provider) where T : UExport => Load(provider) switch
        {
            null => null,
            T t => t,
            _ => throw new ParserException($"Loaded {ToString()} but it was of wrong type")
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad<T>(IFileProvider provider, out T export) where T : UExport
        {
            if (!TryLoad(provider, out var genericExport) || !(genericExport is T cast))
            {
                export = default;
                return false;
            }

            export = cast;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> LoadAsync<T>(IFileProvider provider) where T : UExport => await LoadAsync(provider) as T ??
                                                                                       throw new ParserException($"Loaded {ToString()} but it was of wrong type");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T?> TryLoadAsync<T>(IFileProvider provider) where T : UExport => await TryLoadAsync(provider) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport? Load(IFileProvider provider) => ResolvedObject?.Load(provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad(IFileProvider provider, out UExport export)
        {
            if (ResolvedObject != null)
                return ResolvedObject.TryLoad(provider, out export);

            export = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport> LoadAsync(IFileProvider provider)
        {
            if (ResolvedObject != null)
                return await ResolvedObject.LoadAsync(provider);
            throw new ParserException($"{ToString()} could not be loaded");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport?> TryLoadAsync(IFileProvider provider)
        {
            if (ResolvedObject != null)
            {
                var loadedObj = await ResolvedObject.TryLoadAsync(provider);
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
    public abstract class FObjectResource
    {
        public FName ObjectName;
        public FPackageIndex? OuterIndex;

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
        public long RealSerialOffset;
        public long SerialOffset;
        public bool ForcedExport;
        public bool NotForClient;
        public bool NotForServer;
        public FGuid PackageGuid;
        public uint PackageFlags;
        public bool NotAlwaysLoadedForEditorGame;
        public bool IsAsset;
        public int FirstExportDependency;
        public int SerializationBeforeSerializationDependencies;
        public int CreateBeforeSerializationDependencies;
        public int SerializationBeforeCreateDependencies;
        public int CreateBeforeCreateDependencies;
        public Type ExportType;
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
            TemplateIndex = Ar.Ver >= UE4Version.VER_UE4_TemplateIndex_IN_COOKED_EXPORTS ? new FPackageIndex(Ar) : new FPackageIndex();
            OuterIndex = new FPackageIndex(Ar);
            ObjectName = Ar.ReadFName();
            ObjectFlags = Ar.Read<uint>();

            if (Ar.Ver < UE4Version.VER_UE4_64BIT_EXPORTMAP_SERIALSIZES)
            {
                SerialSize = Ar.Read<int>();
                SerialOffset = Ar.Read<int>();
            }
            else
            {
                SerialSize = Ar.Read<long>();
                SerialOffset = Ar.Read<long>();
            }

            RealSerialOffset = SerialOffset;

            ForcedExport = Ar.ReadBoolean();
            NotForClient = Ar.ReadBoolean();
            NotForServer = Ar.ReadBoolean();
            PackageGuid = Ar.Read<FGuid>();
            PackageFlags = Ar.Read<uint>();
            NotAlwaysLoadedForEditorGame = Ar.Ver < UE4Version.VER_UE4_LOAD_FOR_EDITOR_GAME || Ar.ReadBoolean();
            IsAsset = Ar.Ver >= UE4Version.VER_UE4_COOKED_ASSETS_IN_EDITOR_SUPPORT && Ar.ReadBoolean();

            if (Ar.Ver >= UE4Version.VER_UE4_PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS)
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
        }
    }
}