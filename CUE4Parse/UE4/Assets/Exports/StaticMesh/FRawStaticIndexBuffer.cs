using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FRawStaticIndexBuffer
    {
        public ushort[] Indices16; //LegacyIndices
        public uint[] Indices32;

        public FRawStaticIndexBuffer(FArchive Ar)
        {
            if (Ar.Ver < UE4Version.VER_UE4_SUPPORT_32BIT_STATIC_MESH_INDICES)
            {
                Indices16 = Ar.ReadBulkArray<ushort>(() => Ar.Read<ushort>());
                Indices32 = new uint[0];
            }
            else
            {
                bool is32bit = Ar.ReadBoolean();
                var data = Ar.ReadBulkArray(() => Ar.Read<byte>());

                FByteArchive tempAr = new FByteArchive("IndicesReader", data, Ar.Game, Ar.Ver);


                if (Ar.Game >= EGame.GAME_UE4_25)
                {
                    bool bShouldExpandTo32Bit = Ar.ReadBoolean();
                }

                if (tempAr.Length == 0)
                {
                    Indices16 = new ushort[0];
                    Indices32 = new uint[0];
                    return;
                }

                if (is32bit)
                {
                    int count = (int)tempAr.Length / sizeof(uint);
                    Indices16 = new ushort[0];
                    Indices32 = tempAr.ReadArray(count, () => tempAr.Read<uint>());
                }
                else
                {
                    int count = (int)tempAr.Length / sizeof(ushort);
                    Indices16 = tempAr.ReadArray(count, () => tempAr.Read<ushort>());
                    Indices32 = new uint[0];
                }
                tempAr.Dispose();
            }
        }
    }
}
