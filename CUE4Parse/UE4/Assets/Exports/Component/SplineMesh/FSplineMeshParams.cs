using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;

[StructFallback]
[StructLayout(LayoutKind.Sequential)]
public class FSplineMeshParams : IUStruct
{
    /** Start location of spline, in component space. */
    public FVector StartPos;

    /** Start tangent of spline, in component space. */
    public FVector StartTangent;

    /** X and Y scale applied to mesh at start of spline. */
    public FVector2D StartScale;

    /** Roll around spline applied at start, in radians. */
    public float StartRoll;

    /** Starting offset of the mesh from the spline, in component space. */
    public FVector2D StartOffset;

    /** End location of spline, in component space. */
    public FVector EndPos;

    /** X and Y scale applied to mesh at end of spline. */
    public FVector2D EndScale;

    /** End tangent of spline, in component space. */
    public FVector EndTangent;

    /** Roll around spline applied at end, in radians. */
    public float EndRoll;

    /** Ending offset of the mesh from the spline, in component space. */
    public FVector2D EndOffset;

    public FSplineMeshParams(FStructFallback fallback)
    {
        StartPos = fallback.GetOrDefault("StartPos", new FVector(-1000, 0, 0));
        StartTangent = fallback.GetOrDefault("StartTangent", new FVector(100, 0, 0));
        StartScale = fallback.GetOrDefault("StartScale", new FVector2D(1, 1));
        StartRoll = fallback.GetOrDefault<float>("StartRoll");
        StartOffset = fallback.GetOrDefault<FVector2D>("StartOffset");
        EndPos = fallback.GetOrDefault("EndPos", new FVector(100, 0, 0));
        EndScale = fallback.GetOrDefault("EndScale", new FVector2D(1, 1));
        EndTangent = fallback.GetOrDefault("EndTangent", new FVector(100, 0, 0));
        EndRoll = fallback.GetOrDefault<float>("EndRoll");
        EndOffset = fallback.GetOrDefault<FVector2D>("EndOffset");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FVector SplineEvalDir(float a)
    {
        return SplineEvalTangent(a).GetSafeNormal();
    }

    public FVector SplineEvalTangent(float a)
    {
        var c = (6 * StartPos) + (3 * StartTangent) + (3 * EndTangent) - (6 * EndPos);
        var d = (-6 * StartPos) - (4 * StartTangent) - (2 * EndTangent) + (6 * EndPos);
        var e = StartTangent;

        var a2 = a * a;

        return (c * a2) + (d * a) + e;
    }

    // https://en.wikipedia.org/wiki/Cubic_Hermite_spline
    public FVector SplineEvalPos(float a)
    {
        var a2 = a * a;
        var a3 = a2 * a;

        return (((2 * a3) - (3 * a2) + 1) * StartPos) + ((a3 - (2 * a2) + a) * StartTangent) + ((a3 - a2) * EndTangent) + (((-2 * a3) + (3 * a2)) * EndPos);
    }

    // TODO: uhhhh can be improved?
    public string GetSHAHash()
    {
        var hash = SHA256.Create();
        var data = new byte[160];
        BitConverter.GetBytes(StartPos.X).CopyTo(data, 0);
        BitConverter.GetBytes(StartPos.Y).CopyTo(data, 8);
        BitConverter.GetBytes(StartPos.Z).CopyTo(data, 16);
        BitConverter.GetBytes(StartTangent.X).CopyTo(data, 24);
        BitConverter.GetBytes(StartTangent.Y).CopyTo(data, 32);
        BitConverter.GetBytes(StartTangent.Z).CopyTo(data, 40);
        BitConverter.GetBytes(StartScale.X).CopyTo(data, 48);
        BitConverter.GetBytes(StartScale.Y).CopyTo(data, 56);
        BitConverter.GetBytes(StartRoll).CopyTo(data, 64);
        BitConverter.GetBytes(StartOffset.X).CopyTo(data, 68);
        BitConverter.GetBytes(StartOffset.Y).CopyTo(data, 76);
        BitConverter.GetBytes(EndPos.X).CopyTo(data, 80);
        BitConverter.GetBytes(EndPos.Y).CopyTo(data, 88);
        BitConverter.GetBytes(EndPos.Z).CopyTo(data, 96);
        BitConverter.GetBytes(EndTangent.X).CopyTo(data, 104);
        BitConverter.GetBytes(EndTangent.Y).CopyTo(data, 112);
        BitConverter.GetBytes(EndTangent.Z).CopyTo(data, 120);
        BitConverter.GetBytes(EndScale.X).CopyTo(data, 128);
        BitConverter.GetBytes(EndScale.Y).CopyTo(data, 136);
        BitConverter.GetBytes(EndRoll).CopyTo(data, 144);
        BitConverter.GetBytes(EndOffset.X).CopyTo(data, 148);
        BitConverter.GetBytes(EndOffset.Y).CopyTo(data, 156);
        return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "");
    }
}