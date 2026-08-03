namespace CUE4Parse.UE4.Objects.Chaos;

public interface IChaosClass
{
    public void Serialize(FChaosArchive Ar);
    public static abstract IChaosClass SerializationFactory(FChaosArchive Ar);
}