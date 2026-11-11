namespace CUE4Parse.UE4.Assets.Exports.Rig;

public interface IDnaAsset
{
    Dictionary<string, IRawBase>? Layers { get; }
    RigLogic.RigLogicSnapshot? Snapshot { get; }

    int GetRawControlCount();
    string[] GetRawControlNames();
    string GetRawControlName(int index);
    int GetJointCount();
    string[] GetJointNames();
    string GetJointName(int index);
    RawBehavior? GetBehavior();
}

public static class DnaAssetQueries
{
    public static string[] GetRawControlNames(Dictionary<string, IRawBase>? layers)
    {
        if (layers != null && TryGet(layers, "defn", out RawDefinition defn))
            return defn.RawControlNames;
        return [];
    }

    public static string[] GetJointNames(Dictionary<string, IRawBase>? layers)
    {
        if (layers != null && TryGet(layers, "defn", out RawDefinition defn))
            return defn.JointNames;
        return [];
    }

    public static RawBehavior? GetBehavior(Dictionary<string, IRawBase>? layers)
    {
        if (layers != null && TryGet(layers, "bhvr", out RawBehavior behavior))
            return behavior;
        return null;
    }

    // Layers are stored under their normalized id
    public static bool TryGet<T>(Dictionary<string, IRawBase> layers, string id, out T value) where T : class, IRawBase
    {
        if (layers.TryGetValue(id, out var layer) && layer is T typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }
}
