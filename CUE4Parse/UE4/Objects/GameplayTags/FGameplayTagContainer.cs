using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.GameplayTags
{
    public struct FGameplayTagContainer : IUStruct, IEnumerable<FName>
    {
        public readonly FName[] GameplayTags;

        public FGameplayTagContainer(FAssetArchive Ar)
        {
            GameplayTags = Ar.ReadArray(Ar.ReadFName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FName? GetValue(string category) => GameplayTags.First(it => it.Text.StartsWith(category));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<FName> GetEnumerator() => ((IEnumerable<FName>) GameplayTags).GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GameplayTags.GetEnumerator();
    }
}