using System.Collections;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FN.Objects
{
    public enum EFortConnectivityCubeFace
    {
        Front,
        Left,
        Back,
        Right,
        Upper,
        Lower,
        MAX
    }

    [JsonConverter(typeof(FConnectivityCubeConverter))]
    public class FConnectivityCube : IUStruct
    {
        public readonly BitArray[] Faces = new BitArray[(int) EFortConnectivityCubeFace.MAX];

        public FConnectivityCube(FArchive Ar)
        {
            for (int i = 0; i < Faces.Length; i++)
            {
                // Reference: FArchive& operator<<(FArchive&, TBitArray&)
                var numBits = Ar.Read<int>();
                var numWords = numBits.DivideAndRoundUp(32);
                var data = Ar.ReadArray<int>(numWords);
                Faces[i] = new BitArray(data) { Length = numBits };
            }
        }
    }
}
