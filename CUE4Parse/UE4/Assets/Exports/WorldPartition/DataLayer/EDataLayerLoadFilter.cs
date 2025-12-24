namespace CUE4Parse.UE4.Assets.Exports.WorldPartition.DataLayer;

public enum EDataLayerLoadFilter : byte
{
    /** Data Layer is considered by the client and the server. Client runtime state is replicated. */
    None,
    /** Data Layer is only considered by the client. */
    ClientOnly,
    /** Data layer is only considered by the server. */
    ServerOnly
}
