using System;
using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;


public enum ESplineMeshAxis : int
{
    X,
    Y,
    Z,
};

public class USplineMeshComponent : UStaticMeshComponent 
{
    private FSplineMeshParams? _splineParams;
    public FSplineMeshParams SplineParams {
        get {
            if (_splineParams != null) return _splineParams;
            return _splineParams = GetOrDefault<FSplineMeshParams>("SplineParams", new FSplineMeshParams(new FStructFallback()));
        }
    }

    private ESplineMeshAxis? _forwardAxis;
    public ESplineMeshAxis ForwardAxis {
        get {
            if (_forwardAxis != null) return _forwardAxis.Value;
            _forwardAxis = GetOrDefault<ESplineMeshAxis>("ForwardAxis", ESplineMeshAxis.X);
            return _forwardAxis.Value;
        }
    }
    
    
    private FVector? _splineUpDir;
    public FVector SplineUpDir {
        get {
            if (_splineUpDir != null) return _splineUpDir.Value;
            _splineUpDir = GetOrDefault<FVector>("SplineUpDir", new FVector(0, 0, 1f));
            return _splineUpDir.Value;
        }
    }
    
    private float? _splineBoundaryMin;
    public float SplineBoundaryMin {
        get {
            if (_splineBoundaryMin != null) return _splineBoundaryMin.Value;
            _splineBoundaryMin = GetOrDefault<float>("SplineBoundaryMin");
            return _splineBoundaryMin.Value;
        }
    }

    private float? _splineBoundaryMax;
    public float SplineBoundaryMax {
        get {
            if (_splineBoundaryMax != null) return _splineBoundaryMax.Value;
            _splineBoundaryMax = GetOrDefault<float>("SplineBoundaryMax");
            return _splineBoundaryMax.Value;
        }
    }

    private bool _smoothInterpRollScale;
    public bool bSmoothInterpRollScale {
        get {
            if (_smoothInterpRollScale) return _smoothInterpRollScale;
            _smoothInterpRollScale = GetOrDefault<bool>("bSmoothInterpRollScale");
            return _smoothInterpRollScale;
        }
    }
    
    public override void Deserialize(FAssetArchive Ar, long validPos) 
    {
        base.Deserialize(Ar, validPos);    
        if (Ar.Ver < EUnrealEngineObjectUE4Version.SPLINE_MESH_ORIENTATION) {
            PropertyUtil.Set(this, "ForwardAxis", new EnumProperty("Z"));
            var splineParams = SplineParams;
            splineParams.StartRoll -= MathF.PI / 2;
            splineParams.EndRoll -= MathF.PI / 2;

            splineParams.StartOffset = new FVector2D(-splineParams.StartOffset.Y, splineParams.StartOffset.X);
            splineParams.EndOffset = new FVector2D(-splineParams.EndOffset.Y, splineParams.EndOffset.X);
            PropertyUtil.Set(this, "SplineParams", splineParams);
        }
    }

    public string GetMeshId() 
    {
        // var mesh = GetStaticMesh().Load<UStaticMesh>();
        // if (mesh == null) throw new ObjectNotFoundException("Mesh is null");
        // var meshGuid = mesh?.ObjectGuid ?? new FGuid(0);

        return SplineParams.GetSHAHash();
    }
    
    public FTransform CalcSliceTransform(float distanceAlong) 
    {
        var alpha = ComputeRatioAlongSpline(distanceAlong);

        float MinT = 0f;
        float MaxT = 1f;
        ComputeVisualMeshSplineTRange(ref MinT, ref MaxT);
        return CalcSliceTransformAtSplineOffset(alpha, MinT, MaxT);
    }

    FTransform CalcSliceTransformAtSplineOffset(float alpha, float minT, float maxT) 
    {
        var hermiteAlpha = bSmoothInterpRollScale ? SmoothStep(0.0f, 1.0f, alpha) : alpha;
        
        var splineParams = SplineParams;
        
        FVector splinePos;
        FVector splineDir;


        if (alpha < minT) 
        {
            var startTangent = splineParams.SplineEvalTangent(minT);
            splinePos = splineParams.SplineEvalPos(minT) + (startTangent * (alpha - minT));
            splineDir = startTangent.GetSafeNormal();
        }
        else if (alpha > maxT) 
        {
            var endTangent = splineParams.SplineEvalTangent(maxT);
            splinePos = splineParams.SplineEvalPos(maxT) + (endTangent * (alpha - maxT));
            splineDir = endTangent.GetSafeNormal();
        }
        else 
        {
            splinePos = splineParams.SplineEvalPos(alpha);
            splineDir = splineParams.SplineEvalDir(alpha);
        }

        // base
        var baseXVec = (SplineUpDir ^ splineDir).GetSafeNormal();
        var baseYVec = (splineDir ^ baseXVec).GetSafeNormal();

        // Offset the spline by the desired amount
        var sliceOffset = UnrealMath.Lerp(splineParams.StartOffset, splineParams.EndOffset, hermiteAlpha);
        splinePos += sliceOffset.X * baseXVec;
        splinePos += sliceOffset.Y * baseYVec;
    
        // Apply Roll
        var useRoll = UnrealMath.Lerp(splineParams.StartRoll, splineParams.EndRoll, hermiteAlpha);
        var cosAng = MathF.Cos(useRoll);
        var sinAng = MathF.Sin(useRoll);
        var xVec = (cosAng * baseXVec) - (sinAng * baseYVec);
        var yVec = (cosAng * baseYVec) + (sinAng * baseXVec);

        // Find Scale
        var useScale = UnrealMath.Lerp(splineParams.StartScale, splineParams.EndScale, hermiteAlpha);

        // Build overall transform
        var sliceTransform = new FTransform();
        switch (ForwardAxis) {
            case ESplineMeshAxis.X:
                sliceTransform = new FTransform(splineDir, xVec, yVec, splinePos);
                sliceTransform.Scale3D = new FVector(1, useScale.X, useScale.Y);
                break;
            case ESplineMeshAxis.Y:
                sliceTransform = new FTransform(yVec, splineDir, xVec, splinePos);
                sliceTransform.Scale3D = new FVector(useScale.Y, 1, useScale.X);
                break;
            case ESplineMeshAxis.Z:
                sliceTransform = new FTransform(xVec, yVec, splineDir, splinePos);
                sliceTransform.Scale3D = new FVector(useScale.X, useScale.Y, 1);
                break;
            default:
                Trace.Assert(false);
                break;
        }
        return sliceTransform;
    }

    private void ComputeVisualMeshSplineTRange(ref float minT, ref float maxT)
    {
        var bHasCustomBoundary = !UnrealMath.IsNearlyEqual(SplineBoundaryMin, SplineBoundaryMax);
        if (bHasCustomBoundary)
        {
            var meshBounds = GetStaticMesh().Load<UStaticMesh>()!.RenderData!.Bounds!;
            var boundMin = GetAxisValueRef(meshBounds.Origin - meshBounds.BoxExtent, ForwardAxis);
            var boundMax = GetAxisValueRef(meshBounds.Origin + meshBounds.BoxExtent, ForwardAxis);
            
            var boundMinT = (boundMin - SplineBoundaryMin) / (SplineBoundaryMax - SplineBoundaryMin);
            var boundMaxT = (boundMax - SplineBoundaryMin) / (SplineBoundaryMax - SplineBoundaryMin);
            
            const float MaxSplineExtrapolation = 4.0f;
            minT = MathF.Max(-MaxSplineExtrapolation, boundMinT);
            maxT = MathF.Min(boundMaxT, MaxSplineExtrapolation);
        }
    }

    private float ComputeRatioAlongSpline(float distanceAlong)
    {
        var alpha = 0f;
        var bHasCustomBoundary = !UnrealMath.IsNearlyEqual(SplineBoundaryMin, SplineBoundaryMax);
        if (bHasCustomBoundary)
        {
            var splineLength = SplineBoundaryMax - SplineBoundaryMin;
            if (splineLength > 0)
            {
                alpha = (distanceAlong - SplineBoundaryMin) / splineLength;
            }
        }
        else
        {
            var mesh = GetLoadedStaticMesh();
            if (mesh != null) {
                var meshBounds = mesh!.RenderData!.Bounds;
                var meshMinZ = GetAxisValueRef(ref meshBounds.Origin, ForwardAxis) -
                               GetAxisValueRef(ref meshBounds.BoxExtent, ForwardAxis);
                var meshRangeZ = 2 * GetAxisValueRef(ref meshBounds.BoxExtent, ForwardAxis);

                if (meshRangeZ > UnrealMath.SmallNumber) {
                    alpha = (distanceAlong - meshMinZ) / meshRangeZ;
                }
            }
        }

        return alpha;
    }
    
    public static float GetAxisValueRef(ref FVector vector, ESplineMeshAxis axis)
    {
        return axis switch
        {
            ESplineMeshAxis.X => vector.X,
            ESplineMeshAxis.Y => vector.Y,
            ESplineMeshAxis.Z => vector.Z,
            _ => 0 // should never happen
        };
    }
    
    public static float GetAxisValueRef(FVector vector, ESplineMeshAxis axis)
    {
        return axis switch
        {
            ESplineMeshAxis.X => vector.X,
            ESplineMeshAxis.Y => vector.Y,
            ESplineMeshAxis.Z => vector.Z,
            _ => 0 // should never happen
        };
    }
    
    public static void SetAxisValueRef(ref FVector vector, ESplineMeshAxis axis, float value)
    {
        switch (axis)
        {
            case ESplineMeshAxis.X:
                vector.X = value;
                break;
            case ESplineMeshAxis.Y:
                vector.Y = value;
                break;
            case ESplineMeshAxis.Z:
                vector.Z = value;
                break;
            default:
                Trace.Assert(false);
                break;
        }
    }

    public static float SmoothStep(float a, float b, float x)
    {
        if (x < a)
        {
            return 0.0f;
        }
        else if (x >= b)
        {
            return 1.0f;
        }
        float interpFraction = (x - a) / (b - a);
        return interpFraction * interpFraction * (3.0f - 2.0f * interpFraction);
    }
}
