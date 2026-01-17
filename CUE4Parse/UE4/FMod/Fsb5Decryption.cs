using System;
using System.IO;
using System.Text;
using Serilog;

namespace CUE4Parse.UE4.FMod;

/// <summary>
/// Known encryption keys:
/// 
/// - Godbreakers: 06U8A&w5#PnsW&GA
/// - The Darkest Files: JNM-zHdO49i_s)p&rG8`a:{)GMI6O*U:Jq\"1E8k0£%O*AyxXFL
/// - PAPERHEAD EP0: 666Paperhead999
/// - Delverium Demo: D3lv3rium FTW!
/// - Mr. Nomad Demo: vanillaicecream
/// 
/// Credits to https://github.com/vgmstream/vgmstream/blob/master/src/meta/fsb_keys.h and other sources:
/// - Double Fine Productions: DFm3t4lFTW
/// - DJ Hero 2 (X360): nos71RiT
/// - N++ (PC?): H$#FJa%7gRZZOlxLiN50&g5Q
/// - Slightly Mad Studios (Project CARS, World of Speed): sTOoeJXI2LjK8jBMOk8h5IDRNZl3jq3I
/// - Ghost in the Shell: First Assault (PC): %lAn2{Pi*Lhw3T}@7*!kV=?qS$@iNlJ
/// - RevHeadz Engine Sounds (Mobile): 1^7%82#&5$~/8sz
/// - Dark Souls 3 (PC): FDPrVuT4fAFvdHJYAgyMzRF4EcBAnKg
/// - Need for Speed Shift 2 Unleashed (PC): p&oACY^c4LK5C2v^x5nIO6kg5vNH$tlj
/// - Mortal Kombat X/XL (PC): 996164B5FC0F402983F61F220BB51DC6
/// - Mirror War: Reincarnation of Holiness (PC): logicsounddesignmwsdev
/// - Xian Xia Chuan (PC): gat@tcqs2010
/// - Critter Crunch / Superbrothers: Sword & Sworcery (PC): j1$Mk0Libg3#apEr42mo
/// - Cyphers: @kdj43nKDN^k*kj3ndf02hd95nsl(NJG
/// - Xuan Dou Zhi Wang / King of Combat: Xiayuwu69252.Sonicli81223#$*@*0
/// - Ji Feng Zhi Ren / Kritika Online: kri_tika_5050_
/// - Invisible Inc. (PC?): mint78run52
/// - Guitar Hero 3: 5atu6w4zaw
/// - Supreme Commander 2 (PC): B2A7BB00
/// - Cookie Run: Ovenbreak: ghfxhslrghfxhslr
/// - Monster Jam (PS2): truck/impact/carbody
/// - Sekiro: Shadows Die Twice (PC): G0KTrWjS9syqF7vVD6RaVXlFD91gMgkC
/// - SCP: Unity (PC): BasicEncryptionKey
/// - Worms Rumble Beta (PC): FXnTffGJ9LS855Gc
/// - Bubble Fighter (PC): qjvkeoqkrdhkdckd
/// - Fall Guys (PC) ~2021-11: p@4_ih*srN:UJk&8
/// - Fall Guys (PC) ~2022-07: ,&.XZ8]fLu%caPF+
/// - Fall Guys (PC) ~2023-05: ^*4[hE>K]x90Vj
/// - Achilles: Legends Untold (PC): Achilles_0_15_DpG
/// - Cult of the Lamb Demo (PC): 4FB8CC894515617939F4E1B7D50972D27213B8E6
/// - Signalis (PC): X3EK%Bbga-%Y9HZZ%gkc*C512*$$DhRxWTGgjUG@=rUD
/// - Ash Echoes beta (Android): 281ad163160cfc16f9a22c6755a64fad
/// - Afterimage demo (PC): Aurogon666
/// - Blanc (PC/Switch): IfYouLikeThosesSoundsWhyNotRenumerateTheir2Authors?
/// - Nishuihan Mobile (Android): L36nshM520
/// - Forza Motorsport (PC): Forza2!
/// - JDM: Japanese Drift Master (PC): cbfjZTlUPaZI
/// - Ys Online: The Call of Solum (PC): tkdnsem000
/// - Test Drive: Ferrari Racing Legends (PC): 4DxgpNV3pQLPD6GT7g9Gf6eWU7SXutGQ
/// - Hello Kitty: Island Adventure (iOS): AjaxIsTheGoodestBoy
/// - Rivals of Aether 2 (PC): resoforce
/// - Final Fantasy XV: War for Eos (Android): 3cfe772db5b55b806541d3faf894020e
/// - Forza Motorsport 2023 (PC): aj#$kLucf2lh}eqh
/// - AirRider CrazyRacing (PC): dpdjeoqkr
/// - Wanderstop (PC): weareAbsolutelyUnsure2018
/// - UNBEATABLE Demo (PC): .xW3uXQ8q79yunvMjL6nahLXts9esEXX2VgetuPCxdLrAjUUbZAmB7R*A6KjW24NU_8ifMZ8TC4Qk@_oEsjsK2QLpAaG-Fy!wYKP
/// - Rennsport (PC): ,H9}:p?`bRlQG5_yJ""/L,X_{:=Gs1
/// - Gunner, HEAT, PC! (PC): K50j8B2H4pVUfzt7yxfTprg9wdr9zIH6
/// - Duet Night Abyss (PC) beta: Panshen666
/// </summary>
public class Fsb5Decryption
{
    private static readonly string FSB5Header = "FSB5";
    private static readonly byte[] ReverseBitsTable =
    [
        0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0, 0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
        0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8, 0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
        0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4, 0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
        0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC, 0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
        0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2, 0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
        0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA, 0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
        0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6, 0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
        0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE, 0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
        0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1, 0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
        0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9, 0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
        0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5, 0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
        0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED, 0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
        0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3, 0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
        0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB, 0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
        0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7, 0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
        0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF, 0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF
    ];

    public static bool IsFSB5Header(Stream stream)
    {
        long savedPos = stream.Position;
        Span<byte> header = stackalloc byte[4];
        stream.ReadExactly(header);
        stream.Position = savedPos;
        return Encoding.ASCII.GetString(header) == FSB5Header;
    }

    public static Stream Decrypt(Stream fsbStream, byte[]? key)
    {
        if (key == null || key.Length == 0)
            throw new ArgumentException("FSB5 is encrypted, but encryption key wasn't provided, cannot decrypt", nameof(key));

        const int bufferSize = 65_536;
        byte[] buffer = new byte[bufferSize];
        var decrypted = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, FileOptions.DeleteOnClose);
        long position = 0;

        fsbStream.Position = 0;

        while (position < fsbStream.Length)
        {
            int bytesToRead = (int) Math.Min(bufferSize, fsbStream.Length - position);
            int bytesRead = fsbStream.Read(buffer, 0, bytesToRead);

            if (bytesRead == 0)
                break;

            for (int i = 0; i < bytesRead; i++)
                buffer[i] = (byte) (ReverseBitsTable[buffer[i]] ^ key[(position + i) % key.Length]);

            decrypted.Write(buffer, 0, bytesRead);
            position += bytesRead;
        }

        decrypted.Position = 0;

        if (!IsFSB5Header(decrypted))
            throw new Exception("Failed to decrypt FSB5, make sure encryption key is correct");

#if DEBUG
        Log.Debug("Decrypted FSB5 successfully");
#endif

        return decrypted;
    }
}
