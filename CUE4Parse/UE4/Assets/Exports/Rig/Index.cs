using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class Index
{
    public string Id;
    public DNAVersion Version;
    public uint Offset;
    public uint Size;

    public Index(FArchiveBigEndian Ar)
    {
        Id = Encoding.UTF8.GetString(Ar.ReadBytes(4));
        Version = new DNAVersion(Ar);
        Offset = Ar.Read<uint>();
        Size = Ar.Read<uint>();
    }
}
