using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FTypeFlexibleTunable(FStructFallback fallback)
{
    public readonly EFlexibleTunableType SetType = fallback.GetOrDefault<EFlexibleTunableType>(nameof(SetType));
    public readonly bool IsExportedToBackend = fallback.GetOrDefault<bool>(nameof(IsExportedToBackend));
    public readonly bool _boolValue = fallback.GetOrDefault<bool>(nameof(_boolValue));
    public readonly float _floatValue = fallback.GetOrDefault<float>(nameof(_floatValue));
    public readonly int _intValue = fallback.GetOrDefault<int>(nameof(_intValue));
    public readonly string _stringValue = fallback.GetOrDefault(nameof(_stringValue), string.Empty);
    public readonly string[] _arrayValue = fallback.GetOrDefault<string[]>(nameof(_arrayValue), []);
}
