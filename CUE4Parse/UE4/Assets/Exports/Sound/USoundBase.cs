using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public abstract class USoundBase : UObject
    {
        protected USoundBase() { }
        protected USoundBase(FObjectExport exportObject) : base(exportObject) { }
    }
}
