using CUE4Parse.UE4.Objects.Core;

namespace CUE4Parse.UE4.Assets.Exports.Chaos;


public class TSerializablePtr<T> 
{
    private T Ptr;
    
    // public TSerializablePtr(TRefCountPtr<T> refCountPtr)
    // {
    //     Ptr = refCountPtr.GetReference();
    // }
}