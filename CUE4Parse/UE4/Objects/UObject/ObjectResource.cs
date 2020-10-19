using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

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

        public readonly Package? Owner;

        public FObjectImport? ImportObject => Index < 0 ? Owner?.ImportMap[-Index - 1] : null;
        public FObjectImport? OuterImportObject => ImportObject?.OuterIndex?.ImportObject ?? ImportObject;
        
        public FObjectExport? ExportObject => Index > 0 ? Owner?.ExportMap[Index - 1] : null;
        
        public bool IsNull => Index == 0;
        public bool IsExport => Index > 0;
        public bool IsImport => Index < 0;

        public string Name => ImportObject?.ObjectName.Text
                              ?? ExportObject?.ObjectName.Text
                              ?? Index.ToString();

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
            return ImportObject?.ObjectName.Text.Insert(0, "Import: ")
                   ?? ExportObject?.ObjectName.Text.Insert(0, "Export: ")
                   ?? Index.ToString();
        }
        
        #region Loading Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport Load() =>
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
        public T Load<T>() where T : UExport =>
            Load<T>(Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));
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
        public T Load<T>(IFileProvider provider) where T : UExport =>
            Load(provider) as T ?? throw new ParserException($"Loaded {ToString()} but it was of wrong type");

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
        public UExport Load(IFileProvider provider) => ImportObject?.Load(provider) ?? ExportObject?.Load(provider) ??
            throw new ParserException("Package was loaded without a IFileProvider");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad(IFileProvider provider, out UExport export)
        {
            var import = ImportObject;
            if (import != null && import.TryLoad(provider, out export))
                return true;
            var exportObj = ExportObject;
            if (exportObj != null && exportObj.TryLoad(provider, out export))
                return true;

            export = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport> LoadAsync(IFileProvider provider)
        {
            var import = ImportObject;
            if (import != null)
                return await import.LoadAsync(provider);
            var exportObj = ExportObject;
            if (exportObj != null)
                return await exportObj.LoadAsync(provider);
            throw new ParserException($"{ToString()} could not be loaded");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport?> TryLoadAsync(IFileProvider provider)
        {
            var import = ImportObject;
            if (import != null)
            {
                var loadedImport = await import.TryLoadAsync(provider);
                if (loadedImport != null)
                    return loadedImport;
            }
            var exportObj = ExportObject;
            if (exportObj != null)
            {
                var loadedExport = await exportObj.TryLoadAsync(provider);
                if (loadedExport != null)
                    return loadedExport;
            }

            return null;
        }
        
        #endregion
    }
    
    
    /// <summary>
    /// Base class for UObject resource types.  FObjectResources are used to store UObjects on disk
    /// via FLinker's ImportMap (for resources contained in other packages) and ExportMap (for resources
    /// contained within the same package)
    /// </summary>
    public abstract class FObjectResource
    {
        public FName ObjectName;
        public FPackageIndex? OuterIndex;
        
        #region Loading Methods
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport Load() =>
            Load(OuterIndex?.Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad(out UExport export)
        {
            var provider = OuterIndex?.Owner?.Provider;
            if (provider == null)
            {
                export = default;
                return false;
            }
            return TryLoad(provider, out export);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Load<T>() where T : UExport =>
            Load<T>(OuterIndex?.Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad<T>(out T export) where T : UExport
        {
            var provider = OuterIndex?.Owner?.Provider;
            if (provider == null)
            {
                export = default;
                return false;
            }
            return TryLoad(provider, out export);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport> LoadAsync() => await LoadAsync(OuterIndex?.Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport?> TryLoadAsync()
        {
            var provider = OuterIndex?.Owner?.Provider;
            if (provider == null) return null;
            return await TryLoadAsync(provider).ConfigureAwait(false);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T> LoadAsync<T>() where T : UExport => await LoadAsync<T>(OuterIndex?.Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T?> TryLoadAsync<T>() where T : UExport
        {
            var provider = OuterIndex?.Owner?.Provider;
            if (provider == null) return null;
            return await TryLoadAsync<T>(provider).ConfigureAwait(false);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Load<T>(IFileProvider provider) where T : UExport =>
            Load(provider) as T ?? throw new ParserException($"Loaded {ToString()} but it was of wrong type");

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

        public abstract UExport Load(IFileProvider provider);

        public abstract bool TryLoad(IFileProvider provider, out UExport export);

        public abstract Task<UExport> LoadAsync(IFileProvider provider);

        public abstract Task<UExport?> TryLoadAsync(IFileProvider provider);
        
        #endregion

        public override string ToString()
        {
            return ObjectName.Text;
        }
    }

    public class FObjectExport : FObjectResource
    {
        public readonly FPackageIndex ClassIndex;
        public readonly FPackageIndex SuperIndex;
        public readonly FPackageIndex TemplateIndex;
        public readonly uint ObjectFlags;
        public readonly long SerialSize;
        public readonly long SerialOffset;
        public readonly bool ForcedExport;
        public readonly bool NotForClient;
        public readonly bool NotForServer;
        public readonly FGuid PackageGuid;
        public readonly uint PackageFlags;
        public readonly bool NotAlwaysLoadedForEditorGame;
        public readonly bool IsAsset;
        public readonly int FirstExportDependency;
        public readonly int SerializationBeforeSerializationDependencies;
        public readonly int CreateBeforeSerializationDependencies;
        public readonly int SerializationBeforeCreateDependencies;
        public readonly int CreateBeforeCreateDependencies;
        public Type ExportType;
        public Lazy<UExport> ExportObject;

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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override UExport Load(IFileProvider provider) => ExportObject.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryLoad(IFileProvider provider, out UExport export)
        {
            try
            {
                export = ExportObject.Value;
                return true;
            }
            catch
            {
                export = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async Task<UExport> LoadAsync(IFileProvider provider) => await Task.FromResult(ExportObject.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async Task<UExport?> TryLoadAsync(IFileProvider provider)
        {
            try
            {
                return await Task.FromResult(ExportObject.Value);
            }
            catch
            {
                return await Task.FromResult<UExport?>(null);
            }
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
        public readonly FName ClassName;

        public FObjectImport(FAssetArchive Ar)
        {
            ClassPackage = Ar.ReadFName();
            ClassName = Ar.ReadFName();
            OuterIndex = new FPackageIndex(Ar);
            ObjectName = Ar.ReadFName();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override UExport Load(IFileProvider provider) => LoadAsync(provider).Result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryLoad(IFileProvider provider, out UExport export)
        {
            export = TryLoadAsync(provider).Result;
            return export != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async Task<UExport> LoadAsync(IFileProvider provider)
        {
            // The needed export is located in another asset
            var outerImport = OuterIndex?.ImportObject ?? throw new ParserException("Outer ImportObject must be not null");
            var pkg = await provider.LoadPackageAsync(outerImport.ObjectName.Text);
            return pkg.GetExport(ObjectName.Text);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override async Task<UExport?> TryLoadAsync(IFileProvider provider)
        {
            // The needed export is located in another asset
            var outerImport = OuterIndex?.ImportObject;
            if (outerImport == null)
                return null;

            var pkg = await provider.TryLoadPackageAsync(outerImport.ObjectName.Text);
            return pkg?.GetExportOrNull(ObjectName.Text);
        }
    }
}