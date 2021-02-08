using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.GameplayTags
{
    [JsonConverter(typeof(FGameplayTagContainerConverter))]
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
    
    public class FGameplayTagContainerConverter : JsonConverter<FGameplayTagContainer>
    {
        public override void WriteJson(JsonWriter writer, FGameplayTagContainer value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            foreach (var tag in value.GameplayTags)
            {
                writer.WriteValue(tag.Text);
            }
            
            writer.WriteEndArray();
        }

        public override FGameplayTagContainer ReadJson(JsonReader reader, Type objectType, FGameplayTagContainer existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}