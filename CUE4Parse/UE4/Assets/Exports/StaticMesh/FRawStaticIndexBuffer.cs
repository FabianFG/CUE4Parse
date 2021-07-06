using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FRawStaticIndexBuffer
    {
        public ushort[] Indices16; // LegacyIndices
        public uint[] Indices32;

        public FRawStaticIndexBuffer(FArchive Ar)
        {
            if (Ar.Ver < UE4Version.VER_UE4_SUPPORT_32BIT_STATIC_MESH_INDICES)
            {
                Indices16 = Ar.ReadBulkArray<ushort>();
                Indices32 = new uint[0];
            }
            else
            {
                var is32bit = Ar.ReadBoolean();
                var data = Ar.ReadBulkArray<byte>();
                var tempAr = new FByteArchive("IndicesReader", data, Ar.Game, Ar.Ver);

                if (Ar.Game >= EGame.GAME_UE4_25)
                {
                    Ar.Position += 4;
                }

                if (tempAr.Length == 0)
                {
                    Indices16 = new ushort[0];
                    Indices32 = new uint[0];
                    return;
                }

                if (is32bit)
                {
                    var count = (int)tempAr.Length / 4;
                    Indices16 = new ushort[0];
                    Indices32 = tempAr.ReadArray<uint>(count);
                }
                else
                {
                    var count = (int)tempAr.Length / 2;
                    Indices16 = tempAr.ReadArray<ushort>(count);
                    Indices32 = new uint[0];
                }
                tempAr.Dispose();
            }
        }
        
        public int this[int i]
        {
            get
            {
                if (Indices32.Length > 0)
                    return (int)Indices32[i];
                if (Indices16.Length > 0)
                    return Indices16[i];
                throw new IndexOutOfRangeException();
            }
        }
    }
}
