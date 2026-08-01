using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.GameTypes.Aion2.Objects;

public class FAion2MapDataFile : FAion2DataFile
{
    
    public FAion2MapDataFile(GameFile file, IFileProvider provider)
    {
        var data = file.SafeRead();
        ArgumentNullException.ThrowIfNull(data);
        FAion2DatFileArchive.DecryptData(data);

        if (!file.Directory.Contains("Data/Map", StringComparison.OrdinalIgnoreCase) && !file.Directory.Contains("Data/WorldMap", StringComparison.OrdinalIgnoreCase))
            return;

        using var Ar = new FAion2DatFileArchive(data, provider.Versions);
        Version = 0;
        Ids = Ar.ReadFString().Split(",");

        var mappings = (provider.MappingsContainer?.MappingsForGame)
            ?? throw new ParserException("Mapping is missing, cannot deserialize");

        var tagData = new FPropertyTagData(file.NameWithoutExtension is "MapData" ? "MapData" : "AionWorldMapExportInfo");

        var tag = new FPropertyTag
        {
            Name = "Data",
            PropertyType = "StructProperty",
            Tag = FAion2PropertyReader.ReadPropertyTagType(Ar, mappings, "StructProperty", tagData, true),
            TagData = tagData,
        };

        Properties.Add(tag);
    }
}
