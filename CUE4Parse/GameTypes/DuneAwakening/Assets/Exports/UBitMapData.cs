using System.Collections;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.DuneAwakening.Assets.Exports;

public class UBitMapData : UObject
{
    public int m_SizeX;
    public int m_SizeY;
    public int m_BitsPerWord;
    public BitArray Data;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        m_SizeX = GetOrDefault<int>(nameof(m_SizeX));
        m_SizeY = GetOrDefault<int>(nameof(m_SizeY));
        m_BitsPerWord = GetOrDefault<int>(nameof(m_BitsPerWord));
        Data = new BitArray(Ar.ReadBytes(Ar.Read<int>() >> 3));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(Data));
        writer.WriteStartObject();
        for (var i = 0; i < m_SizeX; i++)
        {
            for (var j = 0; j < m_SizeY; j++)
            {
                var data = 0;
                for (var k = 0; k < m_BitsPerWord; k++)
                {
                    data |= Data[i * m_SizeY + j] ? 1 << k : 0;
                }

                if (data != 0)
                {
                    writer.WritePropertyName($"[{i}, {j}]");
                    writer.WriteValue(data);
                }
            }
        }
        writer.WriteEndObject();
    }
}
