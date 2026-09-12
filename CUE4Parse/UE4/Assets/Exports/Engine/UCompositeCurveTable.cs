using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Engine;

public class UCompositeCurveTable : UCurveTable
{
    private readonly Dictionary<FName, FRealCurve> _resolvedRows = [];
    private RowRebuildState _rowRebuildState;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        _rowRebuildState = Ar.Game >= GAME_UE6_0 && RowMapStorage.Count == 0
            ? RowRebuildState.Pending
            : RowRebuildState.NotRequired;
    }

    protected override void EnsureRowMap()
    {
        if (_rowRebuildState != RowRebuildState.Pending)
            return;

        if (TryRebuildRows([]))
            return;

        RowMapStorage.Clear();
        _resolvedRows.Clear();
        CurveTableMode = ECurveTableMode.Empty;
        _rowRebuildState = RowRebuildState.CyclicDependency;
        Log.Warning("Cyclic dependency found while rebuilding CompositeCurveTable '{0}'.", GetPathName());
    }

    private bool TryRebuildRows(HashSet<UCompositeCurveTable> activeTables)
    {
        if (_rowRebuildState != RowRebuildState.Pending)
            return _rowRebuildState != RowRebuildState.CyclicDependency;
        if (!activeTables.Add(this))
            return false;

        try
        {
            var parents = new List<UCurveTable>();
            foreach (var parentIndex in GetOrDefault<FPackageIndex[]>("ParentTables", []))
            {
                if (!parentIndex.TryLoad<UCurveTable>(out var parent))
                    continue;

                if (parent is UCompositeCurveTable composite)
                {
                    if (!composite.TryRebuildRows(activeTables))
                        return false;
                }
                else
                {
                    _ = parent.RowMap;
                }
                parents.Add(parent);
            }

            RowMapStorage.Clear();
            _resolvedRows.Clear();

            var curveTableMode = parents.Any(static parent => parent.CurveTableMode == ECurveTableMode.RichCurves)
                ? ECurveTableMode.RichCurves
                : ECurveTableMode.SimpleCurves;
            CurveTableMode = curveTableMode;

            foreach (var parent in parents)
            {
                foreach (var (rowName, rowData) in parent.RowMap)
                {
                    var curve = parent.FindCurve(rowName, false);
                    if (curve is null)
                        continue;

                    RowMapStorage[rowName] = rowData;
                    _resolvedRows[rowName] = curveTableMode == ECurveTableMode.RichCurves && curve is FSimpleCurve simple
                        ? ConvertToRichCurve(simple)
                        : curve;
                }
            }

            _rowRebuildState = RowRebuildState.Complete;
            return true;
        }
        finally
        {
            activeTables.Remove(this);
        }
    }

    protected override FRealCurve? ResolveCurve(FName rowName, FStructFallback rowData)
    {
        if (_rowRebuildState == RowRebuildState.Complete && _resolvedRows.TryGetValue(rowName, out var curve))
            return curve;
        return base.ResolveCurve(rowName, rowData);
    }

    private static FRichCurve ConvertToRichCurve(FSimpleCurve simple)
    {
        var rich = new FRichCurve
        {
            DefaultValue = simple.DefaultValue,
            PreInfinityExtrap = simple.PreInfinityExtrap,
            PostInfinityExtrap = simple.PostInfinityExtrap,
            Keys = simple.Keys.Select(key => new FRichCurveKey(key.Time, key.Value)
            {
                InterpMode = simple.InterpMode
            }).ToArray()
        };
        return rich;
    }

    private enum RowRebuildState
    {
        NotRequired,
        Pending,
        Complete,
        CyclicDependency
    }
}
