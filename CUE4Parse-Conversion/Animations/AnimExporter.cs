using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse_Conversion.Animations.PSA;

namespace CUE4Parse_Conversion.Animations
{
    public class AnimExporter : ExporterBase
    {
        private const int PSA_VERSION = 20100422;

        public readonly string AnimName;
        public readonly List<Anim> AnimSequences;

        public AnimExporter(UAnimSequence originalSeq)
        {
            AnimSequences = new List<Anim>();
            AnimName = originalSeq.Name;

            var Anim = AnimConverter.ConvertAnims(null, originalSeq); // TODO IMPORTANT where's the skeleton?
            if (Anim.Sequences.Count == 0)
            {
                // empty CAnimSet
                return;
            }

            // Determine if CAnimSet will save animations as separate psa files, or all at once
            var OriginalAnim = Anim.GetPrimaryAnimObject();

            if (OriginalAnim == Anim.OriginalAnim || Anim.Sequences.Count == 1)
            {
                // Export all animations in a single file
                DoExportPsa(Anim, OriginalAnim);
            }
            else
            {
                var Skeleton = (USkeleton) Anim.OriginalAnim;

                // Export animations separately, this will happen only when CAnimSet has
                // a few sequences (but more than one)
                var TempAnimSet = new CAnimSet();
                TempAnimSet.CopyAllButSequences(Anim);
                // Now we have a copy of AnimSet, let's set up Sequences array to a single
                // item and export one-by-one
                for (int AnimIndex = 0; AnimIndex < Anim.Sequences.Count; AnimIndex++)
                {
                    var seq = Anim.Sequences[AnimIndex];
                    TempAnimSet.Sequences.Clear();
                    TempAnimSet.Sequences.Add(seq);
                    // Do the export, pass UAnimSequence as the "main" object, so it will be
                    // used as psa file name.
                    DoExportPsa(TempAnimSet, seq.OriginalSequence!);
                }
                // Ensure TempAnimSet destructor will not release Sequences as they are owned by Anim object
                TempAnimSet.Sequences.Clear();
            }
        }

        private void DoExportPsa(CAnimSet anim, UObject originalAnim)
        {
            var Ar = new FArchiveWriter();

            var mainHdr = new VChunkHeader();
            var boneHdr = new VChunkHeader();
            var animHdr = new VChunkHeader();
            var keyHdr = new VChunkHeader();
            var scaleKeysHdr = new VChunkHeader();
            int i;

            mainHdr.TypeFlag = PSA_VERSION;
            Ar.SerializeChunkHeader(mainHdr, "ANIMHEAD");

            int numBones = anim.TrackBoneNames.Length;
            int numAnims = anim.Sequences.Count;

            boneHdr.DataCount = numBones;
            boneHdr.DataSize = FNamedBoneBinary.SIZE;
            Ar.SerializeChunkHeader(boneHdr, "BONENAMES");
            for (i = 0; i < numBones; i++)
            {
                Trace.Assert(anim.TrackBoneNames[i].Text.Length < 64);
                var b = new FNamedBoneBinary
                {
                    Name = anim.TrackBoneNames[i].Text,
                    Flags = 0, // reserved
                    NumChildren = 0, // unknown here
                    ParentIndex = i > 0 ? 0 : -1, // unknown for UAnimSet
                    BonePos = { Length = 1.0f }
                };
                if (i < anim.BonePositions.Length)
                {
                    // The AnimSet has bone transform information, store it in psa file (UE4+)
                    b.BonePos.Position = anim.BonePositions[i].Position;
                    b.BonePos.Orientation = anim.BonePositions[i].Orientation;
                }
                b.Serialize(Ar);
            }

            int framesCount = 0;

            animHdr.DataCount = numAnims;
            animHdr.DataSize = AnimInfoBinary.SIZE;
            Ar.SerializeChunkHeader(animHdr, "ANIMINFO");
            for (i = 0; i < numAnims; i++)
            {
                var s = anim.Sequences[i];
                var a = new AnimInfoBinary
                {
                    Name = s.Name,
                    Group = /*??S.Groups.Length > 0 ? S.Groups[0] :*/ "None",
                    TotalBones = numBones,
                    RootInclude = 0, // unused
                    KeyCompressionStyle = 0, // reserved
                    KeyQuotum = s.NumFrames * numBones, // reserved, but fill with keys count
                    KeyReduction = 0, // reserved
                    TrackTime = s.NumFrames,
                    AnimRate = s.Rate,
                    StartBone = 0, // reserved
                    FirstRawFrame = framesCount, // useless, but used in UnrealEd when importing
                    NumRawFrames = s.NumFrames
                };
                a.Serialize(Ar);

                framesCount += s.NumFrames;
            }

            var requireConfig = false;

            int keysCount = framesCount * numBones;
            keyHdr.DataCount = keysCount;
            keyHdr.DataSize = VQuatAnimKey.SIZE;
            Ar.SerializeChunkHeader(keyHdr, "ANIMKEYS");
            for (i = 0; i < numAnims; i++)
            {
                var s = anim.Sequences[i];
                for (int t = 0; t < s.NumFrames; t++)
                {
                    for (int b = 0; b < numBones; b++)
                    {
                        var bP = new FVector(0, 0, 0); // GetBonePosition() will not alter bP and bO when animation tracks are not exists
                        var bO = new FQuat(0, 0, 0, 1);
                        s.Tracks[b].GetBonePosition(t, s.NumFrames, false, ref bP, ref bO);

                        var k = new VQuatAnimKey
                        {
                            Position = bP,
                            Orientation = bO,
                            Time = 1
                        };
                        // MIRROR_MESH
                        k.Orientation.Y *= -1;
                        k.Orientation.W *= -1;
                        k.Position.Y *= -1;
                        k.Serialize(Ar);
                        keysCount--;

                        // check for user error
                        if (s.Tracks[b].KeyPos.Length == 0 || s.Tracks[b].KeyQuat.Length == 0)
                            requireConfig = true;
                    }
                }
            }
            Trace.Assert(keysCount == 0);

            // UE3 source code reference: UEditorEngine::ImportPSAIntoAnimSet()
            // The function doesn't perform any checks for chunk names etc, so we're very restricted in
            // using very strict order of chunks. If main chunk has version (TypeFlag) at least 20090127,
            // importer will always read "SCALEKEYS" chunk.
            if (PSA_VERSION >= 20090127)
            {
                scaleKeysHdr.DataCount = 0;
                scaleKeysHdr.DataSize = 16; // sizeof(VScaleAnimKey) = FVector + float
                Ar.SerializeChunkHeader(scaleKeysHdr, "SCALEKEYS");
            }

            // psa file is done
            AnimSequences.Add(new Anim(originalAnim.Name + ".psa", Ar.GetBuffer()));
            Ar.Dispose();

            // generate configuration file with extended attributes

            // Get statistics of each bone retargeting mode to see if we need a config or not
            var modeCounts = new int[(int) EBoneRetargetingMode.Count];
            foreach (var mode in anim.BoneModes)
            {
                modeCounts[(int) mode]++;
            }
        }

        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName)
        {
            throw new NotImplementedException();
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