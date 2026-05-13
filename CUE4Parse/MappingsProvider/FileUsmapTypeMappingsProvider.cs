using System;
using System.IO;

namespace CUE4Parse.MappingsProvider;

public class FileUsmapTypeMappingsProvider : UsmapTypeMappingsProvider
{
    private readonly StringComparer? _stringComparer;
    private readonly string _path;
    public string FileName => Path.GetFileName(_path);

    public FileUsmapTypeMappingsProvider(string path, StringComparer? comparer = null)
    {
        _stringComparer = comparer;
        _path = path;
        Load(path, _stringComparer);
    }

    public override void Reload()
    {
        Load(_path, _stringComparer);
    }
}
