using System;
using System.Linq;
using System.Threading;

namespace CUE4Parse.UE4.Wwise;

public static class WwiseVersions
{
    // Global access to wwise version game currently uses
    // Should always change because it's possible for game to use multiple versions
    private static uint _wwiseVersion;
    public static uint Version
    {
        get => Interlocked.CompareExchange(ref _wwiseVersion, 0, 0);
        private set => Interlocked.Exchange(ref _wwiseVersion, value);
    }

    public static void SetVersion(uint version) => Version = version;

    // Credits to https://github.com/bnnm/wwiser/blob/ead1751c0320e5e9b532f80bf738cba5f5d2664e/wwiser/parser/wdefs.py#L22
    public static readonly uint[] BankVersions =
    [
        26, // 0x1A Wwise 2007.3?
        29, // 0x1D Wwise 2007.4?
        34, // 0x22 Wwise 2008.1?
        35, // 0x23 Wwise 2008.2
        36, // 0x24 Wwise 2008.3
        38, // 0x26 Wwise 2008.4
        44, // 0x2C Wwise 2009.1?
        45, // 0x2D Wwise 2009.2?
        46, // 0x2E Wwise 2009.3
        48, // 0x30 Wwise 2010.1
        52, // 0x34 Wwise 2010.2
        53, // 0x35 Wwise 2010.3
        56, // 0x38 Wwise 2011.1
        62, // 0x3E Wwise 2011.2
        65, // 0x41 Wwise 2011.3?
        70, // 0x46 Wwise 2012.1?
        72, // 0x48 Wwise 2012.2
        88, // 0x58 Wwise 2013.1/2
        89, // 0x59 Wwise 2013.2-B?
        112, // 0x70 Wwise 2014.1
        113, // 0x71 Wwise 2015.1
        118, // 0x76 Wwise 2016.1
        120, // 0x78 Wwise 2016.2
        122, // 0x7A Wwise 2017.1-B?
        125, // 0x7D Wwise 2017.1
        126, // 0x7E Wwise 2017.1-B?
        128, // 0x80 Wwise 2017.2
        129, // 0x81 Wwise 2017.2-B?
        132, // 0x84 Wwise 2018.1
        134, // 0x86 Wwise 2019.1
        135, // 0x87 Wwise 2019.2
        136, // 0x88 Wwise 2019.2-B?
        140, // 0x8C Wwise 2021.1
        141, // 0x8D Wwise 2021.1-B?
        144, // 0x90 Wwise 2022.1-B
        145, // 0x91 Wwise 2022.1
        150, // 0x96 Wwise 2023.1
        152, // 0x98 Wwise 2024.1-B
        154, // 0x9A Wwise 2024.1
        160, // 0xA8 Wwise 2025.1.0-B
        168, // 0xA8 Wwise 2025.1.0-B
        169, // 0xA9 Wwise 2025.1.1-B 
        171, // 0xAB Wwise 2025.1.2-B
        172, // 0xAC Wwise 2025.1.3
    ];

    // Versions CUE4Parse currently supports
    // Should be noted that Wwise added support for Unreal Engine around version 100, so we can safely ignore older ones
    // TODO: Test more versions
    public static readonly uint[] SupportedVersions =
    [
        112,    // Dead by Daylight (old)
        113,    // Dead by Daylight (old)
        120,    // Code Vein
        125,    // Ace Combat 7
        132,    // Dead by Daylight (old), Undawn
        134,    // Valorant (old)
        135,    // Dead by Daylight (old), Hot Wheels Unleashed, Tetris Effect
        140,    // Dead by Daylight (old), FNAF Security Breach, Hogwarts Legacy, The Casting of Frank Stone, BLUE PROTOCOL, PAYDAY 3, The Anacrusis, The Outlast Trials, Little Nightmares 3
        145,    // Valorant, Marvel Rivals, FNAF: Secret of the Mimic, 2XKO, Crystal of Atlan, REMATCH
        150,    // Dead by Daylight (old), Splitgate 2, Byte Breakers, Le Dernier Don
        154,    // Off The Grid, Dead by Daylight
        172     // Soglia (Unity)
    ];

    public static bool IsSupported() => SupportedVersions.Contains(Version);
}
