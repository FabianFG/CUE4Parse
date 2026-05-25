using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V1;

public class FOnDemandToc : IOnDemandToc
{
    public string ChunksDirectory => Header.ChunksDirectory;
    public IReadOnlyList<IOnDemandContainerEntry> Containers { get; }

    private readonly FOnDemandTocHeader Header;
    public FTocMeta? TocMeta;
    public FOnDemandTocAdditionalFile[]? AdditionalFiles;
    public FOnDemandTocTagSet[] TagSets;
    
    public FOnDemandToc(FArchive Ar)
    {
        Header = new FOnDemandTocHeader(Ar);
        if (Header.Version >= EOnDemandTocVersion.Meta)
            TocMeta = new FTocMeta(Ar);

        Containers = Ar.ReadArray(() => new FOnDemandContainerEntry(Ar, Header.Version));

        if (Header.Version >= EOnDemandTocVersion.AdditionalFiles)
            AdditionalFiles = Ar.ReadArray(() => new FOnDemandTocAdditionalFile(Ar));
        
        if (Header.Version >= EOnDemandTocVersion.TagSets)
            TagSets = Ar.ReadArray(() => new FOnDemandTocTagSet(Ar));
    }
}