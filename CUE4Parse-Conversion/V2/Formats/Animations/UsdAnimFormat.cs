using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.USD;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.V2.Formats.Animations;

public class UsdAnimFormat : IAnimExportFormat
{
    public string DisplayName => "USD Animation (.usda)";

    public IReadOnlyList<ExportFile> Build(string objectName, ExporterOptions options, CAnimSet animSet)
    {
        var dto = new Skeleton(animSet.Skeleton);
        var root = dto.ToSkelRoot();

        foreach (var sequence in animSet.Sequences)
            sequence.RetargetTracks(animSet.Skeleton);

        var numBones = dto.RefSkeleton.Length;

        var fps = animSet.Sequences.Count > 0 ? animSet.Sequences[0].NumFrames / animSet.Sequences[0].AnimEndTime : 30f;

        var totalFrames = (int) MathF.Ceiling(animSet.TotalAnimTime * fps);

        var tSamples = new UsdValue[totalFrames][];
        var rSamples = new UsdValue[totalFrames][];
        var sSamples = new UsdValue[totalFrames][];

        for (var globalFrame = 0; globalFrame < totalFrames; globalFrame++)
        {
            var time = globalFrame / fps; // global time in seconds

            tSamples[globalFrame] = new UsdValue[numBones];
            rSamples[globalFrame] = new UsdValue[numBones];
            sSamples[globalFrame] = new UsdValue[numBones];

            for (var b = 0; b < numBones; b++)
            {
                // default to rest pose
                var bone = dto.RefSkeleton[b];
                var quat = bone.Transform.Rotation;
                var pos  = bone.Transform.Translation;
                var scale = bone.Transform.Scale3D;

                foreach (var sequence in animSet.Sequences)
                {
                    if (sequence.OriginalSequence.FindTrackForBoneIndex(b) < 0) continue;
                    if (time < sequence.StartPos || time >= sequence.StartPos + sequence.AnimEndTime) continue;

                    var localFrame = (time - sequence.StartPos) * (sequence.NumFrames / sequence.AnimEndTime);
                    sequence.Tracks[b].GetBoneTransform(localFrame, sequence.NumFrames, ref quat, ref pos, ref scale);
                    break;
                }

                if (bone.ParentIndex < 0)
                {
                    scale = FVector.OneVector; // root bone should not be scaled
                }

                // MIRROR_MESH
                tSamples[globalFrame][b] = UsdValue.Tuple(pos.X, -pos.Y, pos.Z);
                rSamples[globalFrame][b] = UsdValue.Tuple(quat.W, -quat.X, quat.Y, -quat.Z);
                sSamples[globalFrame][b] = UsdValue.Tuple(scale.X, scale.Y, scale.Z);
            }
        }

        // Grab joints array from the Skeleton prim so SkelAnimation can reference them
        var joints = root.Children[0].Properties
            .OfType<UsdAttribute>()
            .First(a => a.Name == "joints").Value;

        var animPrim = UsdPrim.Def("SkelAnimation", objectName);
        animPrim.Add(UsdAttribute.Uniform("token[]", "joints", joints));
        animPrim.Add(UsdAttribute.TimeSampled("float3[]", "translations", tSamples));
        animPrim.Add(UsdAttribute.TimeSampled("quatf[]",  "rotations",    rSamples));
        animPrim.Add(UsdAttribute.TimeSampled("half3[]",  "scales",       sSamples));
        root.Add(animPrim);

        // SkelBindingAPI + skel:animationSource must be on the SkelRoot for Blender to pick it up
        root.AddMetadata("prepend apiSchemas", UsdValue.Array(UsdValue.Token("SkelBindingAPI")));
        root.Add(new UsdRelationship("skel:animationSource", animPrim));

        var stage = new UsdStage(root);
        stage.AddMetadata("timeCodesPerSecond", (double) fps);
        stage.AddMetadata("startTimeCode", 0.0);
        stage.AddMetadata("endTimeCode", (double) (totalFrames - 1));
        return [new ExportFile("usda", stage.SerializeToBinary())];
    }
}
