using System.IO;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Readers;

public class FRigHierarchyArchive : FArchive
{
    private readonly FArchive _baseArchive;
    private readonly FName[] _names;

    public FRigHierarchyArchive(FArchive baseArchive, FName[] names) : base(baseArchive.Versions)
    {
        _baseArchive = baseArchive;
        _names = names;
    }

    public override bool CanSeek => _baseArchive.CanSeek;
    public override long Length => _baseArchive.Length;
    public override string Name  =>  _baseArchive.Name;
    public override long Position
    {
        get => _baseArchive.Position;
        set => _baseArchive.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count) => _baseArchive.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _baseArchive.Seek(offset, origin);
    public override object Clone() => new FRigHierarchyArchive(_baseArchive, _names);

    public override FName ReadFName()
    {
        var nameIndex = Read<int>();
#if !NO_FNAME_VALIDATION
        if (nameIndex < 0 || nameIndex >= _names.Length)
        {
            throw new ParserException(this, $"FName could not be read, requested index {nameIndex}, name map size {_names.Length}");
        }
#endif
        return _names[nameIndex];
    }
}
