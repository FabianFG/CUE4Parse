using System.Text;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Readers;

public class FMutableArchive : FArchive
{
    private readonly FArchive _baseArchive;
    private readonly Dictionary<int, object?> _ptrs = [];

    public FMutableArchive(FArchive baseArchive)
    {
        _baseArchive = baseArchive;
        Versions = baseArchive.Versions;
    }

    public override int Read(byte[] buffer, int offset, int count) => _baseArchive.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _baseArchive.Seek(offset, origin);
    public override string ReadFString() => new string(_baseArchive.ReadArray<char>()).Replace("\0", string.Empty);
    public override string ReadString() => Encoding.UTF8.GetString(_baseArchive.ReadArray<byte>());

    public T? ReadPtr<T>() where T : unmanaged => _baseArchive.Read<int>() == -1 ? null : _baseArchive.Read<T>();
    public T? ReadPtr<T>(Func<T> func) where T : class
    {
        var id = _baseArchive.Read<int>();
        if (id == -1) return null;

        if (!_ptrs.TryGetValue(id, out var ptr))
        {
            ptr = func.Invoke();
            _ptrs[id] = ptr;
        }

        if (ptr is T result) return result;

        Log.Warning("Ptr type mismatch, expected {Expected}, got {Actual}", typeof(T), ptr?.GetType());
        return null;
    }

    public T?[] ReadPtrArray<T>(Func<T> getter) where T : class => ReadArray(() => ReadPtr(getter));

    public override bool CanSeek => _baseArchive.CanSeek;
    public override long Length => _baseArchive.Length;
    public override string Name  => _baseArchive.Name;
    public override long Position
    {
        get => _baseArchive.Position;
        set => _baseArchive.Position = value;
    }

    public override object Clone() => new FMutableArchive(_baseArchive);
}
