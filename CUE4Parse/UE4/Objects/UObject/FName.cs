using CUE4Parse.UE4.IO.Objects;

namespace CUE4Parse.UE4.Objects.UObject
{
    public readonly struct FName
    {
        private readonly FNameEntrySerialized Name;
        /** Index into the Names array (used to find String portion of the string/number pair used for display) */
        public readonly int Index;
        /** Number portion of the string/number pair (stored internally as 1 more than actual, so zero'd memory will be the default, no-instance case) */
        public readonly int Number;

        public string Text => Number == 0 ? Name.Name : $"{Name.Name}_{Number - 1}";
        public bool IsNone => Text == null || Text == "None";

        public FName(string name)
        {
            Name = new FNameEntrySerialized(name);
            Index = 0;
            Number = 0;
        }

        public FName(FNameEntrySerialized name, int index, int number)
        {
            Name = name;
            Index = index;
            Number = number;
        }

        public FName(FNameEntrySerialized[] nameMap, int index, int number) : this(nameMap[index], index, number)
        {
        }

        public FName(FMappedName mappedName, FNameEntrySerialized[] nameMap) : this(nameMap, (int) mappedName.NameIndex, (int) mappedName.ExtraIndex)
        {
        }

        public override string ToString()
        {
            return Text;
        }
    }
}