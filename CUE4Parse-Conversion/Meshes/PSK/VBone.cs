using System;
using System.Text;
using CUE4Parse_Conversion.Meshes.Common;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class VBone
    {
        public string Name;
        public uint Flags;
        public int NumChildren;
        public int ParentIndex;
        public VJointPosPsk BonePos;

        public void Serialize(FCustomArchiveWriter writer)
        {
            var boneName = new byte[64];
            var bone = Encoding.UTF8.GetBytes(Name);
            Buffer.BlockCopy(bone, 0, boneName, 0, bone.Length);
            
            writer.Write(boneName);
            writer.Write(Flags);
            writer.Write(NumChildren);
            writer.Write(ParentIndex);
            BonePos.Serialize(writer);
        }
    }
}