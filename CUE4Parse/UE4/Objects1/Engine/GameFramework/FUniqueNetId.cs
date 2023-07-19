namespace CUE4Parse.UE4.Objects.Engine.GameFramework
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
