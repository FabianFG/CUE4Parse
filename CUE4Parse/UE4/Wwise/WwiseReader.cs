using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.UE4.Wwise
{
    public class WwiseReader
    {
        private const uint _AKPK_ID = 0x4B504B41;
        private const uint _BKHD_ID = 0x44484B42;
        private const uint _INIT_ID = 0x54494E49;
        private const uint _DIDX_ID = 0x58444944;
        private const uint _DATA_ID = 0x41544144;
        private const uint _HIRC_ID = 0x43524948;
        private const uint _RIFF_ID = 0x46464952;
        private const uint _STID_ID = 0x44495453;
        private const uint _STMG_ID = 0x474D5453;
        private const uint _ENVS_ID = 0x53564E45;
        private const uint _PLAT_ID = 0x54414C50;
        
        public WwiseReader(FArchive Ar)
        {
            while (Ar.Position < Ar.Length)
            {
                var sectionIdentifier = Ar.Read<uint>();
                var sectionLength = Ar.Read<uint>();
                var position = Ar.Position;
                switch (sectionIdentifier)
                {
                    case _AKPK_ID:
                        break;
                    case _BKHD_ID:
                        break;
                    case _INIT_ID:
                        break;
                    case _DIDX_ID:
                        break;
                    case _DATA_ID:
                        break;
                    case _HIRC_ID:
                        break;
                    case _RIFF_ID:
                        break;
                    case _STID_ID:
                        break;
                    case _STMG_ID:
                        break;
                    case _ENVS_ID:
                        break;
                    case _PLAT_ID:
                        break;
                    default:
#if DEBUG
                        Log.Warning($"Unknown section {sectionIdentifier:X} at {position - sizeof(uint) - sizeof(uint)}");
#endif
                        break;
                }
                
                if (Ar.Position != position + sectionLength)
                {
                    var shouldBe = position + sectionLength;
#if DEBUG
                    Log.Warning($"Didn't read 0x{sectionIdentifier:X} correctly (at {Ar.Position}, should be {shouldBe})");
#endif
                    Ar.Position = shouldBe;
                }
            }
        }
    }
}