using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.UObject;
using static CUE4Parse.Tests.Fixtures.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures;

public class FixtureStructuredAssetTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void DataTableDeserializesCustomRows(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var table = LoadExport<UDataTable>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/DataTables/DT_AllProperties.uasset",
            "DT_AllProperties");

        Assert.Equal("FixtureTableRow", table.RowStructName);
        Assert.Equal(["Alpha", "Beta", "Defaults"], table.RowMap.Keys.Select(name => name.Text).Order().ToArray());

        Assert.True(table.TryGetDataTableRow("Alpha", StringComparison.Ordinal, out var alpha));
        Assert.Equal(0x12345678, alpha.Get<int>("Number"));
        Assert.Equal(0x1122334455667788, alpha.Get<long>("LargeNumber"));
        Assert.Equal("Alpha_CUE4Parse_日本語_äöü", alpha.Get<string>("Message"));
        Assert.EndsWith("Alpha", alpha.Get<FName>("Kind").Text, StringComparison.Ordinal);
        AssertNested(alpha.Get<FStructFallback>("Nested"),
            101, "NestedAlpha", 1.25f, -2.5f, 3.75f, true);
        Assert.Equal([11, 22, 33, 44], alpha.Get<int[]>("Values"));

        Assert.True(table.TryGetDataTableRow("Beta", StringComparison.Ordinal, out var beta));
        Assert.Equal(-202020, beta.Get<int>("Number"));
        Assert.Equal(-9007199254740991, beta.Get<long>("LargeNumber"));
        Assert.Equal("Beta_ß_水_🚀", beta.Get<string>("Message"));
        Assert.EndsWith("Beta", beta.Get<FName>("Kind").Text, StringComparison.Ordinal);
        AssertNested(beta.Get<FStructFallback>("Nested"),
            -202, "NestedBeta", -10, 20, -30, false);
        Assert.Equal([-7, 0, 7, int.MaxValue], beta.Get<int[]>("Values"));

        Assert.True(table.TryGetDataTableRow("Defaults", StringComparison.Ordinal, out var defaults));
        Assert.Equal(0, defaults.GetOrDefault<int>("Number"));
        Assert.Equal(0, defaults.GetOrDefault<long>("LargeNumber"));
        Assert.Equal(string.Empty, defaults.GetOrDefault("Message", string.Empty));
        Assert.EndsWith("None", defaults.GetOrDefault<FName>("Kind").Text, StringComparison.Ordinal);
        Assert.Empty(defaults.GetOrDefault<int[]>("Values", []));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void CompositeDataTableAppliesParentsInOrder(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var table = LoadExport<UCompositeDataTable>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/DataTables/DT_Composite.uasset",
            "DT_Composite");

        Assert.Equal(["DT_AllProperties", "DT_Overrides"],
            table.Get<FPackageIndex[]>("ParentTables").Select(static parent => parent.Name));
        Assert.Equal("FixtureTableRow", table.RowStructName);
        Assert.Equal(["Alpha", "Beta", "Defaults", "Gamma"],
            table.RowMap.Keys.Select(name => name.Text).Order().ToArray());

        Assert.True(table.TryGetDataTableRow("Alpha", StringComparison.Ordinal, out var alpha));
        Assert.Equal(0x12345678, alpha.Get<int>("Number"));
        Assert.True(table.TryGetDataTableRow("Beta", StringComparison.Ordinal, out var beta));
        Assert.Equal(8080, beta.Get<int>("Number"));
        Assert.Equal("Composite override", beta.Get<string>("Message"));
        Assert.EndsWith("Maximum", beta.Get<FName>("Kind").Text, StringComparison.Ordinal);
        Assert.True(table.TryGetDataTableRow("Gamma", StringComparison.Ordinal, out var gamma));
        Assert.Equal(303030303030, gamma.Get<long>("LargeNumber"));
        Assert.Equal([3, 0, 3], gamma.Get<int[]>("Values"));

        var overrides = LoadExport<UDataTable>(provider,
            "CUE4ParseFixtures/Content/Fixtures/DataTables/DT_Overrides.uasset", "DT_Overrides");
        Assert.Equal(["Beta", "Gamma"],
            overrides.RowMap.Keys.Select(static name => name.Text).Order().ToArray());
        Assert.True(overrides.TryGetDataTableRow("Beta", StringComparison.Ordinal, out var directBeta));
        Assert.Equal(8080, directBeta.Get<int>("Number"));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void CurveTablesDeserializeAndEvaluate(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var simple = LoadCurveTable(provider, "CT_Simple");
        Assert.Equal(ECurveTableMode.SimpleCurves, simple.CurveTableMode);
        Assert.Equal(["Constant", "Linear"], simple.RowMap.Keys.Select(name => name.Text).Order().ToArray());

        var linear = Assert.IsType<FSimpleCurve>(simple.FindCurve(new FName("Linear")));
        Assert.Equal(10.0f, linear.Eval(1.0f));
        // Unreal removes the redundant collinear middle key while cooking.
        Assert.Equal([-1.0f, 2.0f], linear.Keys.Select(key => key.Time).ToArray());
        Assert.Equal([-10.0f, 20.0f], linear.Keys.Select(key => key.Value).ToArray());

        var constant = Assert.IsType<FSimpleCurve>(simple.FindCurve(new FName("Constant")));
        Assert.Equal(5.0f, constant.Eval(0.5f));

        var rich = LoadCurveTable(provider, "CT_Rich");
        Assert.Equal(ECurveTableMode.RichCurves, rich.CurveTableMode);
        var cubic = Assert.IsType<FRichCurve>(rich.FindCurve(new FName("Cubic")));
        Assert.Equal(ERichCurveExtrapolation.RCCE_Linear, cubic.PreInfinityExtrap);
        Assert.Equal(ERichCurveExtrapolation.RCCE_CycleWithOffset, cubic.PostInfinityExtrap);
        Assert.Equal(3, cubic.Keys.Length);
        Assert.Equal(ERichCurveInterpMode.RCIM_Cubic, cubic.Keys[0].InterpMode);
        Assert.Equal(ERichCurveTangentMode.RCTM_User, cubic.Keys[0].TangentMode);
        Assert.Equal(-1.5f, cubic.Keys[0].ArriveTangent);
        Assert.Equal(2.25f, cubic.Keys[0].LeaveTangent);

        var composite = LoadExport<UCompositeCurveTable>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Curves/CT_Composite.uasset",
            "CT_Composite");
        Assert.Equal(["CT_Simple", "CT_SimpleOverrides"],
            composite.Get<FPackageIndex[]>("ParentTables").Select(static parent => parent.Name));
        Assert.Equal(["CompositeOnly", "Constant", "Linear"],
            composite.RowMap.Keys.Select(name => name.Text).Order().ToArray());
        Assert.Equal(300.0f, Assert.IsType<FSimpleCurve>(
            composite.FindCurve(new FName("Linear"))).Eval(1.0f));
        Assert.Equal(-25.0f, Assert.IsType<FSimpleCurve>(
            composite.FindCurve(new FName("CompositeOnly"))).Eval(2.5f));

        var overrides = LoadCurveTable(provider, "CT_SimpleOverrides");
        Assert.Equal(["CompositeOnly", "Linear"],
            overrides.RowMap.Keys.Select(static name => name.Text).Order().ToArray());
        Assert.Equal(300.0f,
            Assert.IsType<FSimpleCurve>(overrides.FindCurve(new FName("Linear"))).Eval(1.0f));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void StandaloneCurvesDeserializeAndEvaluate(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        var floatAsset = LoadExport<UCurveFloat>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Curves/Curve_Float.uasset",
            "Curve_Float");
        var floatCurve = floatAsset.Get<FRichCurve>("FloatCurve");
        Assert.Equal([-2.0f, 0.0f, 3.0f], floatCurve.Keys.Select(key => key.Time).ToArray());
        Assert.Equal([-4.0f, 2.0f, 11.0f], floatCurve.Keys.Select(key => key.Value).ToArray());
        Assert.Equal(-7.0f, floatCurve.Eval(-3.0f));

        var vector = LoadExport<UCurveVector>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Curves/Curve_Vector.uasset",
            "Curve_Vector");
        Assert.Equal(3, vector.FloatCurves.Length);
        Assert.Equal([5.5f, 11.0f, 16.5f], vector.FloatCurves.Select(curve => curve.Eval(1.0f)).ToArray());

        var color = LoadExport<UCurveLinearColor>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Curves/Curve_LinearColor.uasset",
            "Curve_LinearColor");
        var midpoint = color.GetUnadjustedLinearColorValue(0.5f);
        Assert.Equal((1.0f, 0.875f, 0.75f, 0.625f), (midpoint.R, midpoint.G, midpoint.B, midpoint.A));
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void StringTableUsesStableNamespaceKeysAndValues(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var table = LoadExport<UStringTable>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/StringTables/ST_Fixed.uasset",
            "ST_Fixed");

        Assert.Equal("CUE4ParseFixtures.StringTable", table.StringTable.TableNamespace);
        Assert.Equal(3, table.StringTable.KeysToEntries.Count);
        Assert.Equal("Hello from Frankfurt", table.StringTable.KeysToEntries["Greeting"]);
        Assert.Equal("Fixture 日本語 Ω äöü", table.StringTable.KeysToEntries["Unicode"]);
        Assert.Equal(string.Empty, table.StringTable.KeysToEntries["Empty"]);
    }

    private static UCurveTable LoadCurveTable(CUE4Parse.FileProvider.DefaultFileProvider provider, string name) =>
        LoadExport<UCurveTable>(
            provider,
            $"CUE4ParseFixtures/Content/Fixtures/Curves/{name}.uasset",
            name);
}
