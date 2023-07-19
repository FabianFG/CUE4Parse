using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.IO.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public enum FNameComparisonMethod : byte
    {
        Text,
        Index
    }

    [JsonConverter(typeof(FNameConverter))]
    public readonly struct FName : IComparable<FName>
    {
        private readonly FNameEntrySerialized _name;
        /** Index into the Names array (used to find String portion of the string/number pair used for display) */
        public readonly int Index;
        /** Number portion of the string/number pair (stored internally as 1 more than actual, so zero'd memory will be the default, no-instance case) */
        public readonly int Number;

        public string Text => Number == 0 ? PlainText : $"{PlainText}_{Number - 1}";
        public string PlainText
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _name.Name ?? "None";
        }
        public bool IsNone => Text == "None";

        public readonly FNameComparisonMethod ComparisonMethod;

        public FName(string? name, int index = 0, int number = 0, FNameComparisonMethod compare = FNameComparisonMethod.Text)
        {
            _name = new FNameEntrySerialized(name);
            Index = index;
            Number = number;
            ComparisonMethod = compare;
        }

        public FName(FNameEntrySerialized name, int index, int number, FNameComparisonMethod compare = FNameComparisonMethod.Index)
        {
            _name = name;
            Index = index;
            Number = number;
            ComparisonMethod = compare;
        }

        public FName(FNameEntrySerialized[] nameMap, int index, int number, FNameComparisonMethod compare = FNameComparisonMethod.Index) : this(nameMap[index], index, number, compare) { }

        public FName(FMappedName mappedName, FNameEntrySerialized[] nameMap, FNameComparisonMethod compare = FNameComparisonMethod.Index) : this(nameMap, (int) mappedName.NameIndex, (int) mappedName.ExtraIndex, compare) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FName(string s) => new(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FName a, FName b) => a.ComparisonMethod switch
        {
            FNameComparisonMethod.Index => a.Index == b.Index && a.Number == b.Number,
            FNameComparisonMethod.Text => string.Equals(a.Text, b.Text, StringComparison.OrdinalIgnoreCase), // Case sensitive in editor, case insensitive in runtime
            _ => throw new ArgumentOutOfRangeException()
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FName a, FName b) => !(a == b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FName a, int b) => a.Index == b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FName a, int b) => a.Index != b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FName a, uint b) => a.Index == b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FName a, uint b) => a.Index != b;

        public override bool Equals(object? obj) => obj is FName other && this == other;

        public override int GetHashCode() => ComparisonMethod == FNameComparisonMethod.Text ? StringComparer.OrdinalIgnoreCase.GetHashCode(Text.GetHashCode()) : HashCode.Combine(Index, Number);

        public int CompareTo(FName other) => string.Compare(Text, other.Text, StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Text;
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
