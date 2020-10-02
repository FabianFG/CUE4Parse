using CUE4Parse.UE4.Objects.PakFile;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Pak.Reader
{
    public abstract class FPakArchive : FArchive
    {
        public readonly FPakInfo PakInfo;
        public readonly string FileName;

        public FPakArchive(string fileName) : base()
        {
            FileName = fileName;
            PakInfo = new FPakInfo(this);
            // me no comprendo todos los capas de classe Fabian help me :'(
        }
    }
}
