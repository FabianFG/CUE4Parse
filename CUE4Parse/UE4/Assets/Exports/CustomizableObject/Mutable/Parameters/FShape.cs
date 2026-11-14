using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FShape
{
    public FVector Position;
    public FVector Up;
    public FVector Side;
    public FVector Size;
    public EType Type;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EType : byte
{
    None = 0,
    Ellipse,
    AABox
}
