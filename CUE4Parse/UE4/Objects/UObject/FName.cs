using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.IO.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public enum FNameComparisonMethod : byte
    {
        Index,
        Text
    }
    
    [JsonConverter(typeof(FNameConverter))]
    public readonly struct FName
    {
        private readonly FNameEntrySerialized Name;
        /** Index into the Names array (used to find String portion of the string/number pair used for display) */
        public readonly int Index;
        /** Number portion of the string/number pair (stored internally as 1 more than actual, so zero'd memory will be the default, no-instance case) */
        public readonly int Number;

        public string Text => Number == 0 ? Name.Name : $"{Name.Name}_{Number - 1}";
        public bool IsNone => Text == null || Text == "None";

        public readonly FNameComparisonMethod ComparisonMethod;

        public FName(string name, int index = 0, int number = 0, FNameComparisonMethod compare = FNameComparisonMethod.Text)
        {
            Name = new FNameEntrySerialized(name);
            Index = index;
            Number = number;
            ComparisonMethod = compare;
        }

        public FName(FNameEntrySerialized name, int index, int number, FNameComparisonMethod compare = FNameComparisonMethod.Index)
        {
            Name = name;
            Index = index;
            Number = number;
            ComparisonMethod = compare;
        }

        public FName(FNameEntrySerialized[] nameMap, int index, int number, FNameComparisonMethod compare = FNameComparisonMethod.Index) : this(nameMap[index], index, number, compare)
        {
        }

        public FName(FMappedName mappedName, FNameEntrySerialized[] nameMap, FNameComparisonMethod compare = FNameComparisonMethod.Index) : this(nameMap, (int) mappedName.NameIndex, (int) mappedName.ExtraIndex, compare)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FName a, FName b)
        {
            return a.ComparisonMethod switch
            {
                FNameComparisonMethod.Index => a.Index == b.Index && a.Number == b.Number,
                FNameComparisonMethod.Text => a.Text == b.Text,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FName a, FName b)
        {
            return !(a == b);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FName a, int b)
        {
            return a.Index == b;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FName a, int b)
        {
            return a.Index != b;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FName a, uint b)
        {
            return a.Index == b;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FName a, uint b)
        {
            return a.Index != b;
        }

        public override string ToString()
        {
            return Text;
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
}