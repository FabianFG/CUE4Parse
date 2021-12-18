using System;
using System.Collections.Generic;
using System.Numerics;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Schema2;

namespace CUE4Parse_Conversion.Meshes.glTF
{
    public struct VertexColorXTextureX: IVertexMaterial, IEquatable<VertexColorXTextureX>
    {
        public int MaxColors => 1; // Do we need more?
        public int MaxTextCoords => Constants.MAX_MESH_UV_SETS;

        [VertexAttribute("COLOR_0", EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        // public List<Vector2> TexCoords;
        [VertexAttribute("TEXCOORD_0")] public Vector2 TexCoord0;
        [VertexAttribute("TEXCOORD_1")] public Vector2 TexCoord1;
        [VertexAttribute("TEXCOORD_2")] public Vector2 TexCoord2;
        [VertexAttribute("TEXCOORD_3")] public Vector2 TexCoord3;
        [VertexAttribute("TEXCOORD_4")] public Vector2 TexCoord4;
        [VertexAttribute("TEXCOORD_5")] public Vector2 TexCoord5;
        [VertexAttribute("TEXCOORD_6")] public Vector2 TexCoord6;
        [VertexAttribute("TEXCOORD_7")] public Vector2 TexCoord7;

        public VertexColorXTextureX(Vector4 color, List<Vector2> texCoords)
        {
            Color = color;

            texCoords.Capacity = Math.Max(texCoords.Count, /*MaxTextCoords*/ 8);
            Resize(texCoords, texCoords.Capacity, new Vector2(0, 0));
            TexCoord0 = texCoords[0];
            TexCoord1 = texCoords[1];
            TexCoord2 = texCoords[2];
            TexCoord3 = texCoords[3];
            TexCoord4 = texCoords[4];
            TexCoord5 = texCoords[5];
            TexCoord6 = texCoords[6];
            TexCoord7 = texCoords[7];
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            Color = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            switch (setIndex)
            {
                case 0: TexCoord0 = coord; break;
                case 1: TexCoord1 = coord; break;
                case 2: TexCoord2 = coord; break;
                case 3: TexCoord3 = coord; break;
                case 4: TexCoord4 = coord; break;
                case 5: TexCoord5 = coord; break;
                case 6: TexCoord6 = coord; break;
                case 7: TexCoord7 = coord; break;
            }
        }

        public Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return TexCoord0;
                case 1: return TexCoord1;
                case 2: return TexCoord2;
                case 3: return TexCoord3;
                case 4: return TexCoord4;
                case 5: return TexCoord5;
                case 6: return TexCoord6;
                case 7: return TexCoord7;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public Vector4 GetColor(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        private static void Resize<T>(List<T> list, int size, T val)
        {
            if (size > list.Count)
                while (size - list.Count > 0)
                    list.Add(val);
            else if (size < list.Count)
                while (list.Count - size > 0)
                    list.RemoveAt(list.Count-1);
        }

        public bool Equals(VertexColorXTextureX other)
        {
            return other.Color == Color &&
                other.TexCoord0 == TexCoord0 &&
                other.TexCoord1 == TexCoord1 &&
                other.TexCoord2 == TexCoord2 &&
                other.TexCoord3 == TexCoord3 &&
                other.TexCoord4 == TexCoord4 &&
                other.TexCoord5 == TexCoord5 &&
                other.TexCoord6 == TexCoord6 &&
                other.TexCoord7 == TexCoord7;
        }
    }
}
