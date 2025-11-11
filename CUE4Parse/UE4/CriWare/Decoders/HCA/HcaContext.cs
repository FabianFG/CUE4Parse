using System;
using System.IO;
using System.Text;
using static CUE4Parse.UE4.CriWare.Decoders.HCA.Constants;

namespace CUE4Parse.UE4.CriWare.Decoders.HCA;

internal class HcaContext
{
    public HcaContext(Stream hcaStream)
    {
        if (!IsHeaderValid(hcaStream, out int headerSize))
            throw new Exception("Invalid HCA header.");

        BinaryReader binaryReader = new(hcaStream);
        binaryReader.BaseStream.Position = 0;

        BitReader bitReader = new(binaryReader.ReadBytes(headerSize));

        if ((bitReader.Peek(32) & Mask) == StringToUInt32("HCA"))
        {
            bitReader.Skip(32);
            Version = bitReader.Read(16);
            HeaderSize = bitReader.Read(16);

            headerSize -= 8;
        }
        else
            throw new Exception("Not an HCA file.");

        if (headerSize >= 16 && (bitReader.Peek(32) & Mask) == StringToUInt32("fmt"))
        {
            bitReader.Skip(32);
            ChannelCount = bitReader.Read(8);
            SampleRate = bitReader.Read(24);
            FrameCount = bitReader.Read(32);
            EncoderDelay = bitReader.Read(16);
            EncoderPadding = bitReader.Read(16);

            if (!(ChannelCount >= MinChannels && ChannelCount <= MaxChannels))
                throw new Exception("Invalid channel count.");

            if (FrameCount == 0)
                throw new Exception("Frame count is zero.");

            if (!(SampleRate >= MinSampleRate && SampleRate <= MaxSampleRate))
                throw new Exception("Invalid sample rate.");

            headerSize -= 16;
        }
        else
            throw new Exception("No format chunk.");

        if (headerSize >= 16 && (bitReader.Peek(32) & Mask) == StringToUInt32("comp"))
        {
            bitReader.Skip(32);
            FrameSize = bitReader.Read(16);
            MinResolution = bitReader.Read(8);
            MaxResolution = bitReader.Read(8);
            TrackCount = bitReader.Read(8);
            ChannelConfig = bitReader.Read(8);
            TotalBandCount = bitReader.Read(8);
            BaseBandCount = bitReader.Read(8);
            StereoBandCount = bitReader.Read(8);
            BandsPerHfrGroup = bitReader.Read(8);
            MsStereo = bitReader.Read(8);
            Reserved = bitReader.Read(8);

            headerSize -= 16;
        }
        else if (headerSize >= 12 && (bitReader.Peek(32) & Mask) == StringToUInt32("dec"))
        {
            bitReader.Skip(32);
            FrameSize = bitReader.Read(16);
            MinResolution = bitReader.Read(8);
            MaxResolution = bitReader.Read(8);
            TotalBandCount = bitReader.Read(8) + 1;
            BaseBandCount = bitReader.Read(8) + 1;
            TrackCount = bitReader.Read(4);
            ChannelConfig = bitReader.Read(4);
            StereoType = bitReader.Read(8);

            if (StereoType == 0)
                BaseBandCount = TotalBandCount;
            StereoBandCount = TotalBandCount - BaseBandCount;
            BandsPerHfrGroup = 0;

            headerSize -= 12;
        }
        else
            throw new Exception("No compression or decode chunk.");

        if (headerSize >= 8 && (bitReader.Peek(32) & Mask) == StringToUInt32("vbr"))
        {
            bitReader.Skip(32);
            VbrMaxFrameSize = bitReader.Read(16);
            VbrNoiseLevel = bitReader.Read(16);

            if (!(FrameSize == 0 && VbrMaxFrameSize > 8 && VbrMaxFrameSize <= 511))
                throw new Exception("Invalid frame size.");

            headerSize -= 8;
        }
        else
        {
            VbrMaxFrameSize = 0;
            VbrNoiseLevel = 0;
        }

        if (headerSize >= 6 && (bitReader.Peek(32) & Mask) == StringToUInt32("ath"))
        {
            bitReader.Skip(32);
            AthType = bitReader.Read(16);
        }
        else
            AthType = (Version < Version200) ? 1 : 0;

        if (headerSize >= 16 && (bitReader.Peek(32) & Mask) == StringToUInt32("loop"))
        {
            bitReader.Skip(32);
            LoopStartFrame = bitReader.Read(32);
            LoopEndFrame = bitReader.Read(32);
            LoopStartDelay = bitReader.Read(16);
            LoopEndPadding = bitReader.Read(16);

            LoopFlag = true;

            if (!(LoopStartFrame >= 0 && LoopStartFrame <= LoopEndFrame
                && LoopEndFrame < FrameCount))
                throw new Exception("Invalid loop frames.");

            headerSize -= 16;
        }
        else
        {
            LoopStartFrame = 0;
            LoopEndFrame = 0;
            LoopStartDelay = 0;
            LoopEndPadding = 0;
            LoopFlag = false;
        }

        if (headerSize >= 6 && (bitReader.Peek(32) & Mask) == StringToUInt32("ciph"))
        {
            bitReader.Skip(32);
            CiphType = bitReader.Read(16);

            if (!(CiphType == 0 || CiphType == 1 || CiphType == 56))
                throw new Exception("Invalid cipher type.");
            headerSize -= 6;
        }

        if (headerSize >= 8 && (bitReader.Peek(32) & Mask) == StringToUInt32("rva"))
        {
            bitReader.Skip(32);
            int rvaVolumeInt = bitReader.Read(32);
            RvaVolume = Util.UInt32ToSingle((uint) rvaVolumeInt);

            headerSize -= 8;
        }
        else
            RvaVolume = 1.0F;

        if (headerSize >= 5 && (bitReader.Peek(32) & Mask) == StringToUInt32("comm"))
        {
            bitReader.Skip(32);
            CommentLength = bitReader.Read(8);

            if (CommentLength > headerSize)
                throw new Exception("Comment string out of bounds.");

            StringBuilder commentStringBuilder = new();

            for (int i = 0; i < CommentLength; i++)
            {
                commentStringBuilder.Append(bitReader.Read(8));
            }

            Comment = commentStringBuilder.ToString();

            //headerSize -= 5 + CommentLength;
        }
        else
            CommentLength = 0;

        // IDE0059
        //if (headerSize >= 4 && (bitReader.Peek(32) & Mask) == StringToUInt32("pad"))
        //{
        //    headerSize -= (headerSize - 2);
        //}

        if (FrameSize < MinFrameSize || FrameSize > MaxFrameSize)
            throw new Exception("Invalid frame size.");

        if (Version <= Version200)
        {
            if (MinResolution != 1 || MaxResolution != 15)
                throw new Exception("Incompatible resolution.");
        }

        if (TrackCount == 0)
            TrackCount = 1;

        if (TrackCount > ChannelCount)
            throw new Exception("Invalid track count.");

        if (TotalBandCount > SamplesPerSubframe ||
            BaseBandCount > SamplesPerSubframe ||
            StereoBandCount > SamplesPerSubframe ||
            BaseBandCount + StereoBandCount > SamplesPerSubframe ||
            BandsPerHfrGroup > SamplesPerSubframe)
            throw new Exception("Invalid bands.");

        HfrGroupCount = HeaderCeil2(
            TotalBandCount - BaseBandCount - StereoBandCount,
            BandsPerHfrGroup);

        AthCurve = Ath.Init(AthType, SampleRate);
        CipherTable = Cipher.Init(CiphType, KeyCode);

        int channelsPerTrack = ChannelCount / TrackCount;
        ChannelType[] channelTypes = new ChannelType[MaxChannels];

        for (int i = 0; i < channelTypes.Length; i++)
        {
            channelTypes[i] = ChannelType.Discrete;
        }

        if (StereoBandCount > 0 && channelsPerTrack > 1)
        {
            for (int i = 0; i < TrackCount; i++)
            {
                switch (channelsPerTrack)
                {
                    case 2:
                    case 3:
                        channelTypes[0] = ChannelType.StereoPrimary;
                        channelTypes[1] = ChannelType.StereoSecondary;
                        break;

                    case 4:
                        channelTypes[0] = ChannelType.StereoPrimary;
                        channelTypes[1] = ChannelType.StereoSecondary;
                        if (ChannelConfig == 0)
                        {
                            channelTypes[2] = ChannelType.StereoPrimary;
                            channelTypes[3] = ChannelType.StereoSecondary;
                        }
                        break;

                    case 5:
                        channelTypes[0] = ChannelType.StereoPrimary;
                        channelTypes[1] = ChannelType.StereoSecondary;
                        if (ChannelConfig <= 2)
                        {
                            channelTypes[3] = ChannelType.StereoPrimary;
                            channelTypes[4] = ChannelType.StereoSecondary;
                        }
                        break;

                    case 6:
                    case 7:
                        channelTypes[0] = ChannelType.StereoPrimary;
                        channelTypes[1] = ChannelType.StereoSecondary;
                        channelTypes[4] = ChannelType.StereoPrimary;
                        channelTypes[5] = ChannelType.StereoSecondary;
                        break;

                    case 8:
                        channelTypes[0] = ChannelType.StereoPrimary;
                        channelTypes[1] = ChannelType.StereoSecondary;
                        channelTypes[4] = ChannelType.StereoPrimary;
                        channelTypes[5] = ChannelType.StereoSecondary;
                        channelTypes[6] = ChannelType.StereoPrimary;
                        channelTypes[7] = ChannelType.StereoSecondary;
                        break;

                    default:
                        break;
                }
            }
        }

        Channels = new Channel[ChannelCount];
        for (int i = 0; i < ChannelCount; i++)
        {
            Channels[i] = new Channel
            {
                Type = channelTypes[i],
                CodedCount =
                channelTypes[i] != ChannelType.StereoSecondary ?
                BaseBandCount + StereoBandCount :
                BaseBandCount
            };
        }

        Random = DefaultRandom;

        if (MsStereo > 0)
            throw new Exception();
    }

    public void SetKey(ulong key, ushort subKey)
    {
        if (subKey != 0)
        {
            key = key * (((ulong) subKey << 16) | ((ushort) (~subKey + 2)));
        }

        KeyCode = key;
        CipherTable = Cipher.Init(CiphType, KeyCode);
    }

    public int Version { get; set; }
    public int HeaderSize { get; set; }

    public int ChannelCount { get; set; }
    public int SampleRate { get; set; }
    public int FrameCount { get; set; }
    public int EncoderDelay { get; set; }
    public int EncoderPadding { get; set; }

    public int FrameSize { get; set; }
    public int MinResolution { get; set; }
    public int MaxResolution { get; set; }
    public int TrackCount { get; set; }
    public int ChannelConfig { get; set; }
    public int StereoType { get; set; }
    public int TotalBandCount { get; set; }
    public int BaseBandCount { get; set; }
    public int StereoBandCount { get; set; }
    public int BandsPerHfrGroup { get; set; }
    public int MsStereo { get; set; }
    public int Reserved { get; set; }

    public int VbrMaxFrameSize { get; set; }
    public int VbrNoiseLevel { get; set; }

    public int AthType { get; set; }

    public int LoopStartFrame { get; set; }
    public int LoopEndFrame { get; set; }
    public int LoopStartDelay { get; set; }
    public int LoopEndPadding { get; set; }
    public bool LoopFlag { get; set; }

    public int CiphType { get; set; }
    public ulong KeyCode { get; set; }

    public float RvaVolume { get; set; }

    public int CommentLength { get; set; }
    public string Comment { get; set; } = "";

    public int HfrGroupCount { get; set; }
    public byte[] AthCurve { get; set; }
    public byte[] CipherTable { get; set; }

    public int Random { get; set; }
    public Channel[] Channels { get; set; }

    private static bool IsHeaderValid(Stream hcaStream, out int headerSize)
    {
        BitReader bitReader = new(new BinaryReader(hcaStream).ReadBytes(8));

        headerSize = 0;
        if ((bitReader.Peek(32) & Mask) == StringToUInt32("HCA"))
        {
            bitReader.Skip(32 + 16);
            headerSize = bitReader.Read(16);

            return true;
        }

        return false;
    }

    private static int HeaderCeil2(int a, int b)
    {
        if (b < 1)
            return 0;
        return a / b + ((a % b) > 0 ? 1 : 0);
    }

    private static uint StringToUInt32(string value)
    {
        uint result = 0;
        int bytePos = 3;
        for (int i = 0; i < value.Length; i++)
        {
            result |= (uint) (value[i] << 8 * bytePos--);
        }
        return result;
    }
}
