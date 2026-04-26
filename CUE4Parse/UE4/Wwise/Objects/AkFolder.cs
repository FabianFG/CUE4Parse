using System.Text;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

[JsonConverter(typeof(AkFolderConverter))]
public class AkFolder
{
    public readonly uint Offset;
    public readonly uint Id;
    public string? Name;

    public AkFolder(FWwiseArchive Ar)
    {
        Offset = Ar.Read<uint>();
        Id = Ar.Read<uint>();
    }

    public void PopulateName(FWwiseArchive Ar, long namesOffset)
    {
        Ar.Position = namesOffset + Offset;
        var sb = new StringBuilder();
        while (true)
        {
            var c = Ar.Read<char>();
            if (c == 0x00) break;
            sb.Append(c);
        }
        Name = sb.ToString().Trim();
    }
}
