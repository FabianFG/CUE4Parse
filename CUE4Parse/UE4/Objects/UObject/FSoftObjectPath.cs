using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.OuterWorlds2.Readers;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;
using UExport = CUE4Parse.UE4.Assets.Exports.UObject;

namespace CUE4Parse.UE4.Objects.UObject;

[JsonConverter(typeof(FSoftObjectPathConverter))]
public readonly struct FSoftObjectPath : IUStruct
{
    /** Asset path, patch to a top level object in a package. This is /package/path.assetname */
    public readonly FName AssetPathName;
    /** Optional FString for subobject within an asset. This is the sub path after the : */
    public readonly string SubPathString;

    public readonly IPackage? Owner;

    public FSoftObjectPath(FAssetArchive Ar)
    {
        if (Ar.Ver < EUnrealEngineObjectUE4Version.ADDED_SOFT_OBJECT_PATH || Ar.Game == EGame.GAME_DragonQuestXI)
        {
            var path = Ar.Game != EGame.GAME_DragonQuestXI ? Ar.ReadFString() : Ar.ReadFName().Text;
            AssetPathName = path.SubstringBeforeLast('.');
            SubPathString = path.SubstringAfterLast('.');
            Owner = Ar.Owner;
            return;
        }

        if (!Ar.IsFilterEditorOnly && Ar.Ver >= EUnrealEngineObjectUE5Version.ADD_SOFTOBJECTPATH_LIST
            && Ar.Owner is Package package && package.SoftObjectPaths is { Length: > 0 } softObjectPaths)
        {
            var index = Ar.Read<int>();
            if (index < 0 || index >= softObjectPaths.Length)
            {
                Log.Warning("SoftObjectProperty: Invalid SoftObjectPath index {Index} in package {PackageName}", index, Ar.Name);
            }
            else
            {
                var path = softObjectPaths[index];
                AssetPathName = path.AssetPathName;
                SubPathString = path.SubPathString;
                Owner = Ar.Owner;
            }
            return;
        }

        if (Ar.Game is EGame.GAME_OuterWorlds2 && Ar is FOW2ObjectsArchive OW2Ar)
        {
            while (true)
            {
                var data = Ar.Read<uint>();
                var idktype = (data >> 24) & 0xFF; 
                if (idktype == 0xa9)
                {
                    var path = OW2Ar.Objects.SoftObjectPathStore[(int) (data & 0xFFFFFF)];
                    AssetPathName = path.AssetPathName;
                    SubPathString = path.SubPathString;
                    Owner = OW2Ar.Asset;
                    break;
                }
            }
            return;
        }

        AssetPathName = Ar.Ver >= EUnrealEngineObjectUE5Version.FSOFTOBJECTPATH_REMOVE_ASSET_PATH_FNAMES || Ar.Game == EGame.GAME_TheFirstDescendant ? new FName(new FTopLevelAssetPath(Ar).ToString()) : Ar.ReadFName();
        SubPathString = FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.SoftObjectPathUtf8SubPaths ? Ar.ReadFString() : Ar.ReadFUtf8String();
        Owner = Ar.Owner;
    }

    public FSoftObjectPath(FName assetPathName, string subPathString, IPackage? owner = null)
    {
        AssetPathName = assetPathName;
        SubPathString = subPathString;
        Owner = owner;
    }

    #region Loading Methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UExport Load() =>
        Load(Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryLoad([MaybeNullWhen(false)] out UExport export)
    {
        var provider = Owner?.Provider;
        if (provider == null || AssetPathName.IsNone || string.IsNullOrEmpty(AssetPathName.Text))
        {
            export = null;
            return false;
        }
        return TryLoad(provider, out export);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Load<T>() where T : UExport =>
        Load<T>(Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryLoad<T>([MaybeNullWhen(false)] out T export) where T : UExport
    {
        var provider = Owner?.Provider;
        if (provider == null || AssetPathName.IsNone || string.IsNullOrEmpty(AssetPathName.Text))
        {
            export = null;
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
        if (provider == null || AssetPathName.IsNone || string.IsNullOrEmpty(AssetPathName.Text)) return null;
        return await TryLoadAsync(provider).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<T> LoadAsync<T>() where T : UExport => await LoadAsync<T>(Owner?.Provider ?? throw new ParserException("Package was loaded without a IFileProvider"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<T?> TryLoadAsync<T>() where T : UExport
    {
        var provider = Owner?.Provider;
        if (provider == null || AssetPathName.IsNone || string.IsNullOrEmpty(AssetPathName.Text)) return null;
        return await TryLoadAsync<T>(provider).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Load<T>(IFileProvider provider) where T : UExport =>
        Load(provider) as T ?? throw new ParserException("Loaded SoftObjectProperty but it was of wrong type");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryLoad<T>(IFileProvider provider, [MaybeNullWhen(false)] out T export) where T : UExport
    {
        if (!TryLoad(provider, out var genericExport) || !(genericExport is T cast))
        {
            export = null;
            return false;
        }

        export = cast;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<T> LoadAsync<T>(IFileProvider provider) where T : UExport => await LoadAsync(provider) as T ??
                                                                                   throw new ParserException("Loaded SoftObjectProperty but it was of wrong type");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<T?> TryLoadAsync<T>(IFileProvider provider) where T : UExport => await TryLoadAsync(provider) as T;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UExport Load(IFileProvider provider) => provider.LoadPackageObject(AssetPathName.Text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryLoad(IFileProvider provider, [MaybeNullWhen(false)] out UExport export) =>
        provider.TryLoadPackageObject(AssetPathName.Text, out export);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<UExport> LoadAsync(IFileProvider provider) => await provider.LoadPackageObjectAsync(AssetPathName.Text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<UExport?> TryLoadAsync(IFileProvider provider)
    {
        // TODO: this aint a "Try"
        return await provider.LoadPackageObjectAsync(AssetPathName.Text);
    }
    #endregion

    public override string ToString() => string.IsNullOrEmpty(SubPathString)
        ? AssetPathName.IsNone ? "" : AssetPathName.Text
        : $"{AssetPathName.Text}:{SubPathString}";
}
