using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.UObject;
using UExport = CUE4Parse.UE4.Assets.Exports.UObject;

namespace CUE4Parse.UE4.Objects.UObject
{
    public interface ILoadableObject
    {
        public Type? GetObjectType();
        public UExport? Load();
        public bool TryLoad([MaybeNullWhen(false)] out UExport export);
        public Task<UExport?> LoadAsync();
        public Task<UExport?> TryLoadAsync();
    }
}

// deliberately in the global namespace, so we don't need a using to use them
public static class LoadableObjectExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsA<T>(this ILoadableObject obj) where T : UExport => typeof(T).IsAssignableFrom(obj.GetObjectType());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? Load<T>(this ILoadableObject obj) where T : UExport => obj.Load() as T;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryLoad<T>(this ILoadableObject obj, [MaybeNullWhen(false)] out T export) where T : UExport
    {
        if (obj.TryLoad(out var generic) && generic is T cast)
        {
            export = cast;
            return true;
        }

        export = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<T?> LoadAsync<T>(this ILoadableObject obj) where T : UExport => await obj.LoadAsync().ConfigureAwait(false) as T;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<T?> TryLoadAsync<T>(this ILoadableObject obj) where T : UExport => await obj.TryLoadAsync().ConfigureAwait(false) as T;
}
