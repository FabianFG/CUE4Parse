using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class FRawStaticIndexBuffer() : IDisposable
{
    public ushort[]? Indices16; // LegacyIndices
    public uint[]? Indices32;

    public FRawStaticIndexBuffer(FArchive Ar) : this()
    {
        if (Ar.Ver < EUnrealEngineObjectUE4Version.SUPPORT_32BIT_STATIC_MESH_INDICES)
        {
            Indices16 = Ar.ReadBulkArray<ushort>();
        }
        else
        {
            var is32bit = Ar.ReadBoolean();
            var data = Ar.ReadBulkArray<byte>();
            using var tempAr = new FByteArchive("IndicesReader", data, Ar.Versions);

            if (Ar.Versions["RawIndexBuffer.HasShouldExpandTo32Bit"])
            {
                var bShouldExpandTo32Bit = Ar.ReadBoolean();
            }
            
            if (tempAr.Length == 0)
            {
                tempAr.Dispose();
                return;
            }

            if (is32bit)
            {
                var count = (int)tempAr.Length / 4;
                Indices32 = tempAr.ReadArray<uint>(count);
            }
            else
            {
                var count = (int)tempAr.Length / 2;
                Indices16 = tempAr.ReadArray<ushort>(count);
            }

            if (Ar.Game == EGame.GAME_PlayerUnknownsBattlegrounds && Indices16 is not null)
            {
                var cur = 0;
                for (var i = 0; i < Indices16.Length; i++)
                {
                    cur += (short)Indices16[i];
                    Indices16[i] = (ushort)cur;
                }
            }

        }
    }

    public int Length
    {
        get
        {
            if (Indices32 is not null)
                return Indices32.Length;
            if (Indices16 is not null)
                return Indices16.Length;
            return 0;
        }
    }

    public int this[int i]
    {
        get
        {
            if (Indices32 is not null)
                return (int)Indices32[i];
            if (Indices16 is not null)
                return Indices16[i];
            throw new IndexOutOfRangeException();
        }
    }

    public int this[long i]
    {
        get
        {
            if (Indices32 is not null)
                return (int)Indices32[i];
            if (Indices16 is not null)
                return Indices16[i];
            throw new IndexOutOfRangeException();
        }
    }

    public void Dispose()
    {
        if (Indices16 is not null)
        {
            Array.Clear(Indices16);
            Indices16 = null;
        }

        if (Indices32 is not null)
        {
            Array.Clear(Indices32);
            Indices32 = null;
        }
    }
}