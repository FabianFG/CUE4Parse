namespace CUE4Parse.UE4.FMod.Enums;

public enum EWaveformLoadingMode
{
    WaveformLoadingMode_LoadInMemory = 0x0,
    WaveformLoadingMode_DecompressInMemory = 0x1,
    WaveformLoadingMode_StreamFromDisk = 0x2,
    WaveformLoadingMode_Undefined = 0x3,
    WaveformLoadingMode_Max = 0x4
}
