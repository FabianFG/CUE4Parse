namespace CUE4Parse.UE4.Objects.UObject
{
    public class FUniqueNetId
    {
        public string Type;
        public string Contents;

        public FUniqueNetId(string type, string contents)
        {
            Type = type;
            Contents = contents;
        }
    }
}