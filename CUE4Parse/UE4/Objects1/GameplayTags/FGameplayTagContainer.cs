using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.GameplayTags
{
    public readonly struct FGameplayTagContainer : IUStruct, IEnumerable<FName>
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

        public override string ToString() => string.Join(", ", GameplayTags);
    }
    
    public static class FGameplayTagContainerUtility
    {
        public static bool TryGetGameplayTag(this IEnumerable<FName> gameplayTags, string startWith, out FName gameplayTag)
        {
            foreach (var tag in gameplayTags)
            {
                if (tag.IsNone || !tag.Text.StartsWith(startWith)) continue;
                
                gameplayTag = tag;
                return true;
            }
            
            gameplayTag = default;
            return false;
        }
        
        public static IList<string> GetAllGameplayTags(this IEnumerable<FName> gameplayTags, params string[] startWith)
        {
            var ret = new List<string>();
            foreach (var tag in gameplayTags)
            {
                if (tag.IsNone) continue;
                foreach (string s in startWith)
                {
                    if (!tag.Text.StartsWith(s)) continue;
                    ret.Add(tag.Text);
                }
            }
            return ret;
        }
    }
}