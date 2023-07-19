using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using UExport = CUE4Parse.UE4.Assets.Exports.UObject;

namespace CUE4Parse.UE4.Objects.UObject
{
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
            if (Ar.Ver < EUnrealEngineObjectUE4Version.ADDED_SOFT_OBJECT_PATH)
            {
                var path = Ar.ReadFString();
                throw new ParserException(Ar, $"Asset path \"{path}\" is in short form and is not supported, nor recommended");
            }

            AssetPathName = Ar.Ver >= EUnrealEngineObjectUE5Version.FSOFTOBJECTPATH_REMOVE_ASSET_PATH_FNAMES ? new FName(new FTopLevelAssetPath(Ar).ToString()) : Ar.ReadFName();
            SubPathString = Ar.ReadFString();
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
            Load(provider) as T ?? throw new ParserException("Loaded SoftObjectProperty but it was of wrong type");

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
                                                                                       throw new ParserException("Loaded SoftObjectProperty but it was of wrong type");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<T?> TryLoadAsync<T>(IFileProvider provider) where T : UExport => await TryLoadAsync(provider) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport Load(IFileProvider provider) => provider.LoadObject(AssetPathName.Text);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad(IFileProvider provider, out UExport export) =>
            provider.TryLoadObject(AssetPathName.Text, out export);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport> LoadAsync(IFileProvider provider) => await provider.LoadObjectAsync(AssetPathName.Text);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UExport?> TryLoadAsync(IFileProvider provider) =>
            await provider.TryLoadObjectAsync(AssetPathName.Text);
        #endregion

        public override string ToString() => string.IsNullOrEmpty(SubPathString)
            ? (AssetPathName.IsNone ? "" : AssetPathName.Text)
            : $"{AssetPathName.Text}:{SubPathString}";
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
}
