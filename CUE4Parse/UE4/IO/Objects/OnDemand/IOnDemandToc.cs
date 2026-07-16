namespace CUE4Parse.UE4.IO.Objects.OnDemand;

public interface IOnDemandToc
{
    public string ChunksDirectory { get; }
    public IReadOnlyList<IOnDemandContainerEntry> Containers { get; }
}
