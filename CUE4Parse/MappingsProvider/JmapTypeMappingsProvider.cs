using System;
using System.IO;

namespace CUE4Parse.MappingsProvider;

public sealed class JmapTypeMappingsProvider : FileUsmapTypeMappingsProvider
{
    public JmapTypeMappingsProvider(string path, StringComparer? comparer = null) : base(path, comparer) { }

    public override void Load(string path, StringComparer? comparer = null)
    {
        MappingsForGame = File.Exists(path) ? new JmapParser(path, comparer).Mappings : null;
    }

    public override void Load(byte[] bytes, StringComparer? comparer = null)
    {
        MappingsForGame = new JmapParser(bytes, comparer).Mappings;
    }
}
