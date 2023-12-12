using System.Collections;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FN.Objects;

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
public class FConnectivityCube : IUStruct, ISerializable
{
    public readonly BitArray[] Faces = new BitArray[(int) EFortConnectivityCubeFace.MAX];
    private readonly int[][] Data = new int[(int) EFortConnectivityCubeFace.MAX][];

    public FConnectivityCube(FArchive Ar)
    {
        for (var i = 0; i < Faces.Length; i++)
        {
            // Reference: FArchive& operator<<(FArchive&, TBitArray&)
            var numBits = Ar.Read<int>();
            var numWords = numBits.DivideAndRoundUp(32);
            Data[i] = Ar.ReadArray<int>(numWords);
            Faces[i] = new BitArray(Data[i]) { Length = numBits };
        }
    }

    public void Serialize(FArchiveWriter Ar)
    {
        for (var i = 0; i < Faces.Length; i++)
        {
            Ar.Write(Faces[i].Length); // numBits
            Ar.SerializeEnumerable(Data[i], Ar.Write);
        }
    }
}