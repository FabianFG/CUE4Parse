using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.Animations
{
    public class AnimExporter : ExporterBase
    {
        public readonly List<Anim> AnimSequences;

        private AnimExporter(UObject export, ExporterOptions options) : base(export, options)
        {
            AnimSequences = new List<Anim>();
        }

        private AnimExporter(ExporterOptions options, USkeleton skeleton, UObject export, CAnimSet animSet)
            : this(export, options)
        {
            if (animSet.Sequences.Count == 0) return;

            // Determine if CAnimSet will save animations as separate psa files, or all at once
            var originalAnim = animSet.GetPrimaryAnimObject();
            if (originalAnim == animSet.OriginalAnim || animSet.Sequences.Count == 1)
            {
                DoExportPsa(animSet, 0);
            }
            else
            {
                //var skeleton = (USkeleton) anim.OriginalAnim;

                // Export animations separately, this will happen only when CAnimSet has
                // a few sequences (but more than one)
                var tempAnimSet = new CAnimSet();
                tempAnimSet.CopyAllButSequences(animSet);
                // Now we have a copy of AnimSet, let's set up Sequences array to a single
                // item and export one-by-one
                for (int animIndex = 0; animIndex < animSet.Sequences.Count; animIndex++)
                {
                    var seq = animSet.Sequences[animIndex];
                    tempAnimSet.Sequences.Clear();
                    tempAnimSet.Sequences.Add(seq);
                    // Do the export, pass UAnimSequence as the "main" object, so it will be
                    // used as psa file name.
                    DoExportPsa(tempAnimSet, animIndex);
                }
                // Ensure TempAnimSet destructor will not release Sequences as they are owned by Anim object
                tempAnimSet.Sequences.Clear();
            }
        }

        private AnimExporter(ExporterOptions options, USkeleton skeleton, UAnimSequence? animSequence = null)
            : this(options, skeleton, animSequence != null ? animSequence : skeleton, skeleton.ConvertAnims(animSequence))
        {

        }

        private AnimExporter(ExporterOptions options, USkeleton skeleton, UAnimMontage? animMontage = null)
            : this(options, skeleton, animMontage != null ? animMontage : skeleton, skeleton.ConvertAnims(animMontage))
        {

        }

        private AnimExporter(ExporterOptions options, USkeleton skeleton, UAnimComposite? animComposite = null)
            : this(options, skeleton, animComposite != null ? animComposite : skeleton, skeleton.ConvertAnims(animComposite))
        {

        }

        public AnimExporter(UAnimSequence animSequence, ExporterOptions options) : this(options, animSequence.Skeleton.Load<USkeleton>()!, animSequence) { }
        public AnimExporter(UAnimMontage animMontage, ExporterOptions options) : this(options, animMontage.Skeleton.Load<USkeleton>()!, animMontage) { }
        public AnimExporter(UAnimComposite animComposite, ExporterOptions options) : this(options, animComposite.Skeleton.Load<USkeleton>()!, animComposite) { }

        private void DoExportPsa(CAnimSet anim, int seqIdx)
        {
            var Ar = new FArchiveWriter();

            var mainHdr = new VChunkHeader();
            var boneHdr = new VChunkHeader();
            var animHdr = new VChunkHeader();
            var keyHdr = new VChunkHeader();
            var scaleKeysHdr = new VChunkHeader();
            int i;

            mainHdr.TypeFlag = Constants.PSA_VERSION;
            Ar.SerializeChunkHeader(mainHdr, "ANIMHEAD");

            int numBones = anim.TrackBonesInfo.Length;
            int numAnims = anim.Sequences.Count;

            boneHdr.DataCount = numBones;
            boneHdr.DataSize = Constants.FNamedBoneBinary_SIZE;
            Ar.SerializeChunkHeader(boneHdr, "BONENAMES");
            for (i = 0; i < numBones; i++)
            {
                Trace.Assert(anim.TrackBonesInfo[i].Name.Text.Length < 64);
                var bone = new FNamedBoneBinary
                {
                    Name = anim.TrackBonesInfo[i].Name.Text,
                    Flags = 0, // reserved
                    NumChildren = 0, // unknown here
                    ParentIndex = i > 0 ? 0 : -1, // unknown for UAnimSet?? WHAT???
                    BonePos = { Length = 1.0f }
                };
                if (i < anim.BonePositions.Length)
                {
                    // The AnimSet has bone transform information, store it in psa file (UE4+)
                    bone.BonePos.Position = anim.BonePositions[i].Translation;
                    bone.BonePos.Orientation = anim.BonePositions[i].Rotation;
                }
                bone.Serialize(Ar);
            }

            int framesCount = 0;

            animHdr.DataCount = numAnims;
            animHdr.DataSize = Constants.ANIM_INFO_SIZE;
            Ar.SerializeChunkHeader(animHdr, "ANIMINFO");
            for (i = 0; i < numAnims; i++)
            {
                var sequence = anim.Sequences[i];
                var animInfo = new AnimInfoBinary
                {
                    Name = sequence.Name,
                    Group = /*??S.Groups.Length > 0 ? S.Groups[0] :*/ "None",
                    TotalBones = numBones,
                    RootInclude = 0, // unused
                    KeyCompressionStyle = 0, // reserved
                    KeyQuotum = sequence.NumFrames * numBones, // reserved, but fill with keys count
                    KeyReduction = 0, // reserved
                    TrackTime = sequence.NumFrames,
                    AnimRate = sequence.Rate,
                    StartBone = 0, // reserved
                    FirstRawFrame = framesCount, // useless, but used in UnrealEd when importing
                    NumRawFrames = sequence.NumFrames
                };
                animInfo.Serialize(Ar);

                framesCount += sequence.NumFrames;
            }

            var requireConfig = false; // TODO

            int keysCount = framesCount * numBones;
            keyHdr.DataCount = keysCount;
            keyHdr.DataSize = Constants.VQuatAnimKey_SIZE;
            Ar.SerializeChunkHeader(keyHdr, "ANIMKEYS");
            for (i = 0; i < numAnims; i++)
            {
                var sequence = anim.Sequences[i];
                for (int frame = 0; frame < sequence.NumFrames; frame++)
                {
                    for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
                    {
                        var bonePosition = FVector.ZeroVector; // GetBonePosition() will not alter bP and bO when animation tracks are not exists
                        var boneOrientation = FQuat.Identity;
                        var useMeYouIdiot = FVector.OneVector;
                        sequence.Tracks[boneIndex].GetBoneTransform(frame, sequence.NumFrames, ref boneOrientation, ref bonePosition, ref useMeYouIdiot);

                        var key = new VQuatAnimKey
                        {
                            Position = bonePosition,
                            Orientation = boneOrientation,
                            Time = 1
                        };
                        // MIRROR_MESH
                        key.Orientation.Y *= -1;
                        key.Orientation.W *= -1;
                        key.Position.Y *= -1;
                        key.Serialize(Ar);
                        keysCount--;

                        // check for user error
                        if (sequence.Tracks[boneIndex].KeyPos.Length == 0 || sequence.Tracks[boneIndex].KeyQuat.Length == 0)
                            requireConfig = true;
                    }
                }
            }
            Trace.Assert(keysCount == 0);

            // UE3 source code reference: UEditorEngine::ImportPSAIntoAnimSet()
            // The function doesn't perform any checks for chunk names etc, so we're very restricted in
            // using very strict order of chunks. If main chunk has version (TypeFlag) at least 20090127,
            // importer will always read "SCALEKEYS" chunk.
            if (Constants.PSA_VERSION >= 20090127)
            {
                keysCount = framesCount * numBones;
                scaleKeysHdr.DataCount = keysCount;
                scaleKeysHdr.DataSize = Constants.VScaleAnimKey_SIZE;
                Ar.SerializeChunkHeader(scaleKeysHdr, "SCALEKEYS");
                for (i = 0; i < numAnims; i++)
                {
                    var sequence = anim.Sequences[i];
                    for (int frame = 0; frame < sequence.NumFrames; frame++)
                    {
                        for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
                        {
                            var boneScale = FVector.OneVector;

                            if (frame < sequence.Tracks[boneIndex].KeyScale.Length)
                                boneScale = sequence.Tracks[boneIndex].KeyScale[frame];

                            var key = new VScaleAnimKey
                            {
                                ScaleVector = boneScale,
                                Time = 1
                            };
                            key.Serialize(Ar);
                            keysCount--;

                            // check for user error
                            if (sequence.Tracks[boneIndex].KeyScale.Length == 0)
                                requireConfig = true;
                        }
                    }
                }
                Trace.Assert(keysCount == 0);
            }

            // psa file is done
            AnimSequences.Add(new Anim($"{PackagePath}_SEQ{seqIdx}.psa", Ar.GetBuffer()));
            Ar.Dispose();

            // generate configuration file with extended attributes

            // Get statistics of each bone retargeting mode to see if we need a config or not
            // var modeCounts = new int[(int) EBoneRetargetingMode.Count];
            // foreach (var mode in anim.BoneModes)
            // {
            //     modeCounts[(int) mode]++;
            // }
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
        {
            var b = false;
            label = string.Empty;
            savedFilePath = PackagePath;
            if (AnimSequences.Count == 0) return b;

            var outText = "SEQ ";
            for (var i = 0; i < AnimSequences.Count; i++)
            {
                b |= AnimSequences[i].TryWriteToDir(baseDirectory, out label, out savedFilePath);
                outText += $"{i} ";
            }

            label = outText + $"as '{savedFilePath.SubstringAfterWithLast('.')}' for '{ExportName}'";
            return b;
        }

        public override bool TryWriteToZip(out byte[] zipFile)
        {
            throw new NotImplementedException();
        }

        public override void AppendToZip()
        {
            throw new NotImplementedException();
        }
    }
}
