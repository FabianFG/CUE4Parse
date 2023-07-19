using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class FURL
    {
        /** Protocol, i.e. "unreal" or "http". */
        public string Protocol;

        /** Optional hostname, i.e. "204.157.115.40" or "unreal.epicgames.com", blank if local. */
        public string Host;

        /** Optional host port. */
        public int Port;

        public bool Valid;

        /** Map name, i.e. "SkyCity", default is "Entry". */
        public string Map;

        /** Options. */
        public string[] Op;

        /** Portal to enter through, default is "". */
        public string Portal;

        public FURL(FArchive Ar)
        {
            Protocol = Ar.ReadFString();
            Host = Ar.ReadFString();
            Map = Ar.ReadFString();
            Portal = Ar.ReadFString();
            Op = Ar.ReadArray(Ar.ReadFString);
            Port = Ar.Read<int>();
            Valid = Ar.ReadBoolean();
        }
    }
}