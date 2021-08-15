namespace CUE4Parse.UE4.Objects.UObject
{
    // Not an engine class, this inherits the UClass engine class to keep things simple 
    public class UScriptClass : UClass
    {
        public UScriptClass(string className)
        {
            Name = className;
        }
    }
}