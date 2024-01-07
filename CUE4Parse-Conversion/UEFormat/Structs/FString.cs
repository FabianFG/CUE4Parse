using System;
using System.Buffers;
using System.Text;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.UEFormat.Structs;

public readonly struct FString(string Text) : ISerializable
{
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(Text.Length);
        
        var padded = new byte[Text.Length];
        var bytes = Encoding.UTF8.GetBytes(Text); 
        Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
        Ar.Write(padded);
    }
}