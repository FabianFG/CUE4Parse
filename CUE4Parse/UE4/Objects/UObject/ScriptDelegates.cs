using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    /// <summary>
    /// Script delegate base class.
    /// </summary>
    public class FScriptDelegate
    {
        /// <summary>
        /// The object bound to this delegate, or null if no object is bound
        /// </summary>
        public FPackageIndex Object;

        /// <summary>
        /// Name of the function to call on the bound object
        /// </summary>
        public FName FunctionName;

        public FScriptDelegate(FAssetArchive Ar)
        {
            Object = new FPackageIndex(Ar);
            FunctionName = Ar.ReadFName();
        }

        public FScriptDelegate(FPackageIndex obj, FName functionName)
        {
            Object = obj;
            FunctionName = functionName;
        }
    }

    /// <summary>
    /// Script multi-cast delegate base class
    /// </summary>
    public class FMulticastScriptDelegate
    {
        /// <summary>
        /// Ordered list functions to invoke when the Broadcast function is called
        /// </summary>
        public FScriptDelegate[] InvocationList;

        public FMulticastScriptDelegate(FAssetArchive Ar)
        {
            InvocationList = Ar.ReadArray(() => new FScriptDelegate(Ar));
        }

        public FMulticastScriptDelegate(FScriptDelegate[] invocationList)
        {
            InvocationList = invocationList;
        }
    }
}