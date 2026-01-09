namespace CUE4Parse.UE4.Assets.Exports.WorldPartition.DataLayer;

public enum EDataLayerRuntimeState : byte
{
    // Unloaded
    Unloaded,

    // Loaded (meaning loaded but not visible)
    Loaded,

    // Activated (meaning loaded and visible)
    Activated
}
