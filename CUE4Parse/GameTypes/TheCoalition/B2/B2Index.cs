using System.Collections.Generic;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.TheCoalition.B2;

public class B2Index
{
    private const uint B2_INDEX_MAGIC = 0x32424354; // 'TCB2'
    public uint Magic;

    private static long previousBlock = 0;
    private static byte check = 0;
    
    public B2Index(FArchive Ar)
    {
        Magic = Ar.Read<uint>();
        if (Magic != B2_INDEX_MAGIC)
        {
            throw new ParserException(Ar, $"Invalid B2 index magic, expected {B2_INDEX_MAGIC} but got {Magic}");
        }

        Ar.Position = 0x44;

        var filesSection = Ar.Read<long>();
        var files = Ar.Read<uint>();

        Ar.Position = 0x5C;

        var namesSection = Ar.Read<long>();
        var entries = Ar.Read<uint>();

        Ar.Position = namesSection;

        for (var i = 0; i < entries; i++)
        {
            var nameOffset = Ar.Read<long>();
            var fileNumber = Ar.Read<uint>();
            var child = Ar.Read<int>();
            if (child > 0) continue;

            var filesOffset = filesSection + fileNumber * 0x10;
            namesSection = Ar.Position;

            Ar.Position = nameOffset;
            var name = ReadStringProper(Ar);

            Ar.Position = filesOffset;
            var blocksSection = Ar.Read<long>();
            var startOffset = Ar.Read<uint>();
            var fileSize = Ar.Read<uint>();

            if (blocksSection == previousBlock)
            {
                if (check == 0)
                {
                    
                }
                else
                {
                    
                }
            }
            
            use_block:
            var blockSize = startOffset + fileSize;
            for (; check < blockSize;)
            {
                
            }
        }
    }
    
    public string ReadStringProper(FArchive Ar)
    {
        List<byte> byteList = new List<byte>();
        byte b;
        while ((b = Ar.Read<byte>()) != 0)
        {
            byteList.Add(b);
        }
        return Encoding.UTF8.GetString(byteList.ToArray());
    }
}