using System.Buffers.Binary;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.CrankcaseAudioREVModelPlayer;

public class CREVSourceModelPlayerParams : IAkPluginParam
{
    public float Gain;
    public float Throttle;
    public float RPM;
    public int Gear;
    public float Velocity;
    public bool EnableShifting;

    public FEngineSimulationControlData EngineSimulationControlData;
    public FAccelDecelModelControlData? AccelDecelModelControlData;
    public FAccelDecelModelControlData_old? AccelDecelModelControlData_old;
    public float Unknown;

    public CREVSourceModelPlayerParams(FArchive Ar, int size)
    {
        Gain = Ar.Read<float>();
        Throttle = Ar.Read<float>();
        RPM = Ar.Read<float>();
        Gear = Ar.Read<int>();
        Velocity = Ar.Read<float>();
        EnableShifting = Ar.Read<int>() != 0;
        EngineSimulationControlData = new FEngineSimulationControlData(Ar);
        if (WwiseVersions.Version >= 132)
        {
            AccelDecelModelControlData = new FAccelDecelModelControlData(Ar);
            Unknown = BinaryPrimitives.ReadSingleBigEndian(Ar.ReadBytes(4));
        }
        else
        {
            AccelDecelModelControlData_old = new FAccelDecelModelControlData_old(Ar);
        }
    }

    public struct FAccelDecelModelControlData_old(FArchive Ar)
    {
        public short EndianStatus = Ar.Read<short>();
        public ushort Size = Ar.Read<ushort>();

        public float DecelVolume_Off = Ar.Read<float>();
        public float DecelVolume_On = Ar.Read<float>();
        public float PopsEnabled = Ar.Read<float>();
        public float PopsVolumeMax = Ar.Read<float>();
        public float PopsVolumeMin = Ar.Read<float>();
        public float PopsFreqMin = Ar.Read<float>();
        public float PopsFreqMax = Ar.Read<float>();
        public float PopsEngineDuck = Ar.Read<float>();
        public float PopRange = Ar.Read<float>();
        public float PopDuration = Ar.Read<float>();
        public float IdleVolume = Ar.Read<float>();
        public float IdleTechnique = Ar.Read<float>();
        public float IdleRampIn = Ar.Read<float>();
    }

    public struct FEngineSimulationControlData(FArchive Ar)
    {
        public short EndianStatus = Ar.Read<short>();
        public ushort Size = Ar.Read<ushort>();

        public float UpShiftDuration = Ar.Read<float>();
        public float UpShiftAttackDuration = Ar.Read<float>();
        public float UpShiftAttackVolumeSpike = Ar.Read<float>();
        public float UpShiftAttackRPM = Ar.Read<float>();
        public float UpShiftAttackThrottleTime = Ar.Read<float>();
        public bool UpShiftWobbleEnabled = Ar.Read<int>() != 0;
        public float UpShiftWobblePitchFreq = Ar.Read<float>();
        public float UpShiftWobblePitchAmp = Ar.Read<float>();
        public float UpShiftWobbleVolFreq = Ar.Read<float>();
        public float UpShiftWobbleVolAmp = Ar.Read<float>();
        public float UpShiftWobbleDuration = Ar.Read<float>();
        public float DownShiftDuration = Ar.Read<float>();
        public float PopDuration = Ar.Read<float>();
        public float ClutchRPMSpike = Ar.Read<float>();
        public float ClutchRPMSpikeDuration = Ar.Read<float>();
        public float ClutchRPMMergeTime = Ar.Read<float>();
    }

    public struct FAccelDecelModelControlData(FArchive Ar)
    {
        public short EndianStatus = Ar.Read<short>();
        public ushort SizeOf = Ar.Read<ushort>();

        public float MasterVolume = Ar.Read<float>();
        public float IdleVolume = Ar.Read<float>();
        public float IdleTechnique = Ar.Read<float>();
        public float IdleRampIn = Ar.Read<float>();
        public bool LowPassEnabled = Ar.Read<int>() != 0;
        public int HarmonicToTrack = Ar.Read<int>();
        public float QFactor = Ar.Read<float>();
        public float FilterDepth = Ar.Read<float>();
        public int CrossfadeDuration = Ar.Read<int>();
        public float RPMSmoothness = Ar.Read<float>();
        public GranularModelControlData[] GranularModelControlData = Ar.ReadArray(2, () => new GranularModelControlData(Ar));
    };

    public struct GranularModelControlData(FArchive Ar)
    {
        public short EndianStatus = Ar.Read<short>();
        public ushort SizeOf = Ar.Read<ushort>();

        public bool isValid = Ar.Read<int>() != 0;
        public float LoadVolumeOff = Ar.Read<float>();
        public float LoadVolumeOn = Ar.Read<float>();
        public float RampVsLoopMaxWetDry = Ar.Read<float>();
        public float RampVsLoopMinWetDry = Ar.Read<float>();
        public float RampVsLoopSensitivity = Ar.Read<float>();
        public int LoopCrossfadeStyle = Ar.Read<int>();
        public int GrainWidth = Ar.Read<int>();
    }
}

