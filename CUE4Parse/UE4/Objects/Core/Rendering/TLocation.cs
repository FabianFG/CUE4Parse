using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Core.Rendering;

/**
 * Describes an object location in the rendering hierarchical spatial hash grid.
 * The location cosists of an integer 3D coordinate and a Level which is derived such that the size of the bounds are at most 1 (integer) unit at that level.
 * Put differently, the Level is calculated as the FloorLog2(Size).
 */
[StructLayout(LayoutKind.Sequential)]
public struct TLocation<T>
{
    public TIntVector3<T> Coord;
    public int Level;
}
