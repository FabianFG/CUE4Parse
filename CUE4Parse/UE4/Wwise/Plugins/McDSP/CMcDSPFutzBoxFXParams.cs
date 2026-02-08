using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins.McDSP;

public class CMcDSPFutzBoxFXParams(FArchive Ar) : IAkPluginParam
{
    public McDSPFutzBoxFXParams Params = new McDSPFutzBoxFXParams(Ar);
}

public struct McDSPFutzBoxFXParams(FArchive Ar)
{
    public McDSPFutzFiltersFXParams Filters = new McDSPFutzFiltersFXParams(Ar);
    public McDSPFutzDistortionFXParams Distortion = new McDSPFutzDistortionFXParams(Ar);
    public McDSPFutzEQFXParams EQ = new McDSPFutzEQFXParams(Ar);
    public McDSPFutzNoiseFXParams Noise = new McDSPFutzNoiseFXParams(Ar);
    public McDSPFutzGateFXParams Gate = new McDSPFutzGateFXParams(Ar);
    public McDSPFutzSIMFXParams SIM = new McDSPFutzSIMFXParams(Ar);
    public McDSPFutzLoFiFXParams LoFi = new McDSPFutzLoFiFXParams(Ar);
    public McDSPGlobalFXParams Global = new McDSPGlobalFXParams(Ar);
};

[JsonConverter(typeof(StringEnumConverter))]
public enum FutzFilterSlope : int
{
    eFutzFilterSlope12 = 0x0,
    eFutzFilterSlope24 = 0x1
}

public struct McDSPFutzFiltersFXParams(FArchive Ar)
{
    public bool bEnable = Ar.Read<byte>() != 0;
    public FutzFilterSlope LPFSlope = Ar.Read<FutzFilterSlope>();
    public float fLPFFreq = Ar.Read<float>();
    public float fLPFQ = Ar.Read<float>();
    public FutzFilterSlope HPFSlope = Ar.Read<FutzFilterSlope>();
    public float fHPFFreq = Ar.Read<float>();
    public float fHPFQ = Ar.Read<float>();
}

[JsonConverter(typeof(StringEnumConverter))]
public enum FutzDistortionMode : int
{
    eFutzDistMode_Sat1 = 0x0,
    eFutzDistMode_Sat2 = 0x1,
    eFutzDistMode_Fuzz = 0x2,
    eFutzDistMode_LoFi = 0x3,
    eFutzDistMode_Soft = 0x4,
    eFutzDistMode_Stun = 0x5,
    eFutzDistMode_Ouch = 0x6,
    eFutzDistMode_Hard = 0x7,
    eFutzDistMode_Nuke = 0x8,
    eFutzDistMode_Clip = 0x9
}

public struct McDSPFutzDistortionFXParams(FArchive Ar)
{
    public bool bEnable = Ar.Read<byte>() != 0;
    public FutzDistortionMode iMode = Ar.Read<FutzDistortionMode>();
    public float fAmount = Ar.Read<float>();
    public float fIntensity = Ar.Read<float>();
    public float fRectify = Ar.Read<float>();
}

[JsonConverter(typeof(StringEnumConverter))]
public enum FutzEQType : int
{
    eFutzEQType_HPF = 0x0,
    eFutzEQType_EQ = 0x1,
    eFutzEQType_LPF = 0x2
}

public struct McDSPFutzEQFXParams(FArchive Ar)
{
    public bool bEnable = Ar.Read<byte>() != 0;
    public FutzEQType FilterType = Ar.Read<FutzEQType>();
    public float fFreq = Ar.Read<float>();
    public float fQ = Ar.Read<float>();
    public float fGain = Ar.Read<float>();
}

public struct McDSPFutzNoiseFXParams(FArchive Ar)
{
    public bool bEnable = Ar.Read<byte>() != 0;
    public float fLevel = Ar.Read<float>();
    public float fLPFFreq = Ar.Read<float>();
    public float fHPFFreq = Ar.Read<float>();
    public float fThresh = Ar.Read<float>();
    public float fRange = Ar.Read<float>();
    public float fRecovery = Ar.Read<float>();
}

[JsonConverter(typeof(StringEnumConverter))]
public enum FutzSIMType : int
{
    eFutzSIM_Vintage_Box_Speaker = 0x0,
    eFutzSIM_Modern_Phone = 0x1,
    eFutzSIM_Answering_Machine = 0x2,
    eFutzSIM_Horrortone = 0x3,
    eFutzSIM_BoomBox_1 = 0x4,
    eFutzSIM_BoomBox_2 = 0x5,
    eFutzSIM_Car_Radio_1 = 0x6,
    eFutzSIM_Car_Radio_2 = 0x7,
    eFutzSIM_Van_Radio = 0x8,
    eFutzSIM_Garbage_Can_1 = 0x9,
    eFutzSIM_Vacuum_Cleaner_Tube_2 = 0xa,
    eFutzSIM_Garbage_Can_2 = 0xb,
    eFutzSIM_Glass_Cup = 0xc,
    eFutzSIM_Glass_Jar_1 = 0xd,
    eFutzSIM_Glass_Jar_2 = 0xe,
    eFutzSIM_Glass_Jar_3 = 0xf,
    eFutzSIM_Guitar_Amp_I = 0x10,
    eFutzSIM_Guitar_Amp_II = 0x11,
    eFutzSIM_Intercom_Small = 0x12,
    eFutzSIM_Metal_Pail_1 = 0x13,
    eFutzSIM_Metal_Pail_2 = 0x14,
    eFutzSIM_Low_Rider = 0x15,
    eFutzSIM_Classic_Headphones_2 = 0x16,
    eFutzSIM_Blue_Megaphone_1 = 0x17,
    eFutzSIM_Blue_Megaphone_2 = 0x18,
    eFutzSIM_PVC_Pipe = 0x19,
    eFutzSIM_Truck_Radio = 0x1a,
    eFutzSIM_Antique_Tube_Radio_1 = 0x1b,
    eFutzSIM_Radio_Tube_1 = 0x1c,
    eFutzSIM_Radio_Tube_2 = 0x1d,
    eFutzSIM_Portable_Radio = 0x1e,
    eFutzSIM_Driver = 0x1f,
    eFutzSIM_Small_Speaker = 0x20,
    eFutzSIM_Tiny_Speaker = 0x21,
    eFutzSIM_Bookshelf_Speaker_1 = 0x22,
    eFutzSIM_Toy_Speaker_1 = 0x23,
    eFutzSIM_Toy_Speaker_2 = 0x24,
    eFutzSIM_Portable_TapeDeck_1 = 0x25,
    eFutzSIM_Portable_TapeDeck_2 = 0x26,
    eFutzSIM_Euro_Phone = 0x27,
    eFutzSIM_Small_Television_1 = 0x28,
    eFutzSIM_Cheapo_Earbuds_2 = 0x29,
    eFutzSIM_Tin_Can = 0x2a,
    eFutzSIM_Toy_Mic = 0x2b,
    eFutzSIM_Vacuum_Cleaner_Tube_1 = 0x2c,
    eFutzSIM_Wash_Tub = 0x2d,
    eFutzSIM_Watercooler_Bottle = 0x2e,
    eFutzSIM_Cheapo_Earbuds_1 = 0x2f,
    eFutzSIM_Classic_Headphones_1 = 0x30,
    eFutzSIM_Crystal_Earbud = 0x31,
    eFutzSIM_Canceling_Headphones = 0x32,
    eFutzSIM_Pillow_Speaker_Alone = 0x33,
    eFutzSIM_Pillow_Speaker_Under_Pillow = 0x34,
    eFutzSIM_Studio_Headphones_1 = 0x35,
    eFutzSIM_Tinny_Earbuds = 0x36,
    eFutzSIM_Emergency_Radio = 0x37,
    eFutzSIM_Mono_BoomBox = 0x38,
    eFutzSIM_Stereo_BoomBox = 0x39,
    eFutzSIM_Clock_Radio = 0x3a,
    eFutzSIM_Cube_Alarm_Clock = 0x3b,
    eFutzSIM_Red_Megaphone = 0x3c,
    eFutzSIM_Stereo_Alarm_Clock = 0x3d,
    eFutzSIM_Antique_Speaker = 0x3e,
    eFutzSIM_Large_Active_Speakers = 0x3f,
    eFutzSIM_Large_Passive_Speakers = 0x40,
    eFutzSIM_Large_Studio_Monitors = 0x41,
    eFutzSIM_Medium_Active_Speakers = 0x42,
    eFutzSIM_Medium_Speakers = 0x43,
    eFutzSIM_Medium_Studio_Monitors = 0x44,
    eFutzSIM_Portable_Speaker = 0x45,
    eFutzSIM_Cylinder_Speakers = 0x46,
    eFutzSIM_Small_Passive_Speakers = 0x47,
    eFutzSIM_Small_Studio_Monitors = 0x48,
    eFutzSIM_Studio_Subwoofer = 0x49,
    eFutzSIM_Surround_Speakers = 0x4a,
    eFutzSIM_Surround_System = 0x4b,
    eFutzSIM_Surround_Subwoofer = 0x4c,
    eFutzSIM_Bookshelf_Speaker_2 = 0x4d,
    eFutzSIM_Station_Phone = 0x4e,
    eFutzSIM_NOS_Telephone_Receiver = 0x4f,
    eFutzSIM_Business_Phone = 0x50,
    eFutzSIM_Business_Speaker_Phone = 0x51,
    eFutzSIM_Cheapo_Handset = 0x52,
    eFutzSIM_Cheapo_Headset = 0x53,
    eFutzSIM_Classic_Desk_Phone = 0x54,
    eFutzSIM_Classic_Headset = 0x55,
    eFutzSIM_Classic_Wall_Phone = 0x56,
    eFutzSIM_Cradle_Phone = 0x57,
    eFutzSIM_Desk_Phone = 0x58,
    eFutzSIM_Desk_Speaker_Phone = 0x59,
    eFutzSIM_Fax_Machine = 0x5a,
    eFutzSIM_Rotary_Desk_Phone = 0x5b,
    eFutzSIM_House_Phone = 0x5c,
    eFutzSIM_House_Speaker_Phone = 0x5d,
    eFutzSIM_Modern_Headset = 0x5e,
    eFutzSIM_Studio_Phone = 0x5f,
    eFutzSIM_Studio_Speaker_Phone = 0x60,
    eFutzSIM_Small_Television_2 = 0x61,
    eFutzSIM_Karaoke_Toy = 0x62,
    eFutzSIM_Analog_Baby_Monitor = 0x63,
    eFutzSIM_Blue_Walkie_Talkie = 0x64,
    eFutzSIM_Cheapo_CB_Radio = 0x65,
    eFutzSIM_Digital_Baby_Monitor = 0x66,
    eFutzSIM_External_CB_Speaker = 0x67,
    eFutzSIM_Modern_CB_Radio = 0x68,
    eFutzSIM_Modern_Handheld_CB = 0x69,
    eFutzSIM_Modern_Walkie_Talkie = 0x6a,
    eFutzSIM_Tiny_Handheld_CB = 0x6b,
    eFutzSIM_Toy_Walkie_Talkie = 0x6c,
    eFutzSIM_Vintage_Handheld_CB = 0x6d,
    eFutzSIM_Wireless_Intercom = 0x6e,
    eFutzSIM_Candlestick_Phone = 0x6f,
    eFutzSIM_Black_Transistor_Radio = 0x70,
    eFutzSIM_Silver_Transistor_Radio = 0x71,
    eFutzSIM_Globe_Radio = 0x72,
    eFutzSIM_Antique_Tube_Radio_2 = 0x73,
    eFutzSIM_Classic_Cell_Phone = 0x74,
    eFutzSIM_Cheapo_Cell_Phone = 0x75,
    eFutzSIM_Beat_Up_Cell_Phone = 0x76,
    eFutzSIM_Awesome_Cell_Phone = 0x77,
    eFutzSIM_Awesome_Cell_Phone_Speaker = 0x78,
    eFutzSIM_Free_Headphone_Clips = 0x79,
    eFutzSIM_Portable_DVD_Player = 0x7a,
    eFutzSIM_Cool_Headphones = 0x7b,
    eFutzSIM_Guitar_Toy = 0x7c,
    eFutzSIM_Kids_Pod_Toy = 0x7d,
    eFutzSIM_Cash_Register_Toy = 0x7e,
    eFutzSIM_Shopping_Cart_Toy = 0x7f,
    eFutzSIM_Two_Inch_Speaker = 0x80,
    eFutzSIM_One_Inch_Speaker = 0x81,
    eFutzSIM_Tiny_Piezo_Speaker = 0x82,
    eFutzSIM_Cell_Phone_Toy = 0x83,
    eFutzSIM_Large_Flip_Phone_Toy = 0x84,
    eFutzSIM_Small_Flip_Phone_Toy = 0x85,
    eFutzSIM_Learning_Toy = 0x86,
    eFutzSIM_Large_Work_Surface_Toy = 0x87,
    eFutzSIM_Red_Pocket_Radio = 0x88,
    eFutzSIM_Cool_Earbuds = 0x89,
    eFutzSIM_Gamer_Headset = 0x8a,
    eFutzSIM_Nameless_Earbuds = 0x8b,
    eFutzSIM_Quality_Earbuds = 0x8c,
    eFutzSIM_Mid_Grade_Headphones = 0x8d,
    eFutzSIM_Studio_Headphones_2 = 0x8e,
    eFutzSIM_In_Ear_Headphones = 0x8f,
    eFutzSIM_Broken_Cell_Phone = 0x90,
    eFutzSIM_Mini_Video_Camera = 0x91,
    eFutzSIM_Medium_Flat_Panel_Television = 0x92,
    eFutzSIM_Large_Flat_Panel_Television = 0x93,
    eFutzSIM_Iso_Headphones = 0x94,
    eFutzSIM_Extra_Iso_Headphones = 0x95,
    eFutzSIM_Classic_4_x_4_Vehicle = 0x96,
    eFutzSIM_Modern_Compact_Vehicle = 0x97,
    eFutzSIM_Ultra_Hybrid_Vehicle = 0x98,
    eFutzSIM_Flip_Phone = 0x99,
    eFutzSIM_Flip_Phone_Speaker = 0x9a,
    eFutzSIM_Pocket_Cell_Phone = 0x9b,
    eFutzSIM_Pocket_Cell_Phone_Speaker = 0x9c,
    eFutzSIM_Classic_Bar_Phone = 0x9d,
    eFutzSIM_Cheapo_Cell_Phone_Speaker = 0x9e,
    eFutzSIM_Classic_Television = 0x9f,
    eFutzSIM_Modern_Widescreen_Television = 0xa0,
    eFutzSIM_Large_CRT_Television = 0xa1,
    eFutzSIM_AM_BoomBox = 0xa2,
    eFutzSIM_Cylinder_Speakers_Bass = 0xa3
};

public struct McDSPFutzSIMFXParams(FArchive Ar)
{
    public bool bEnable = Ar.Read<byte>() != 0;
    public FutzSIMType iType = Ar.Read<FutzSIMType>();
    public float fTuning = Ar.Read<float>();
}

public struct McDSPFutzGateFXParams(FArchive Ar)
{
    public bool bEnable = Ar.Read<byte>() != 0;
    public float fThreshold = Ar.Read<float>();
    public float fRange = Ar.Read<float>();
    public float fAttack = Ar.Read<float>();
    public float fHold = Ar.Read<float>();
    public float fRelease = Ar.Read<float>();
}

[JsonConverter(typeof(StringEnumConverter))]
public enum FutzBitDepthType : int
{
    eFutzLoFi_BitDepth_24 = 0x0,
    eFutzLoFi_BitDepth_16 = 0x1,
    eFutzLoFi_BitDepth_15 = 0x2,
    eFutzLoFi_BitDepth_14 = 0x3,
    eFutzLoFi_BitDepth_13 = 0x4,
    eFutzLoFi_BitDepth_12 = 0x5,
    eFutzLoFi_BitDepth_11 = 0x6,
    eFutzLoFi_BitDepth_10 = 0x7,
    eFutzLoFi_BitDepth_9 = 0x8,
    eFutzLoFi_BitDepth_8 = 0x9,
    eFutzLoFi_BitDepth_7 = 0xa,
    eFutzLoFi_BitDepth_6 = 0xb,
    eFutzLoFi_BitDepth_5 = 0xc,
    eFutzLoFi_BitDepth_4 = 0xd,
    eFutzLoFi_BitDepth_3 = 0xe,
    eFutzLoFi_BitDepth_2 = 0xf
};

public struct McDSPFutzLoFiFXParams(FArchive Ar)
{
    public bool bEnable = Ar.Read<byte>() != 0;
    public FutzBitDepthType iBitDepthType = Ar.Read<FutzBitDepthType>();
    public uint iDownSampleIndex = Ar.Read<uint>();
    public float fFilter = Ar.Read<float>();
}

public struct McDSPGlobalFXParams(FArchive Ar)
{
    public float fInputGain = Ar.Read<float>();
    public float fOutputGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
    public float fBalance = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
}
