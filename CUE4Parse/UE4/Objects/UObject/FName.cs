namespace CUE4Parse.UE4.Objects.UObject
{
    public readonly struct FName
    {
        private readonly FNameEntry Name;
        /** Index into the Names array (used to find String portion of the string/number pair used for display) */
        public readonly int Index;
        /** Number portion of the string/number pair (stored internally as 1 more than actual, so zero'd memory will be the default, no-instance case) */
        public readonly int Number;

        public string Text => Number == 0 ? Name.Name : $"{Name.Name}_{Number - 1}";
        public bool IsNone => Text == null || Text == "None";

        public FName(FNameEntry name, int index, int number)
        {
            Name = name;
            Index = index;
            Number = number;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}