using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.Tests;

public class DummyGameFile : GameFile
{
    public override bool IsEncrypted => false;
    public override CompressionMethod CompressionMethod => CompressionMethod.None;

    public override byte[] Read() => throw new NotImplementedException();
    public override FArchive CreateReader() => throw new NotImplementedException();
}

public class DummyFileProvider : AbstractFileProvider
{
    public DummyFileProvider()
    {
        Files.AddFiles(new Dictionary<string, GameFile>
        {
            { "ProjectName/ProjectName.uproject", new DummyGameFile() }
        });
    }
}
