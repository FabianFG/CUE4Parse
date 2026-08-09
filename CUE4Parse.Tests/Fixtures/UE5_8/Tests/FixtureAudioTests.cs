using System.Buffers.Binary;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse_Conversion.Sounds;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureAudioTests
{
    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void InlineAndStreamingPcmDecodeToExpectedWaveShape(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        AssertWave(provider, "SW_Inline", expectedLooping: false, expectedSeconds: 1);
        AssertWave(provider, "SW_Streaming", expectedLooping: true, expectedSeconds: 6);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void SoundCuePreservesMixerGraphAndWaveReferences(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var cue = LoadExport<USoundCue>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Audio/SC_Fixture.uasset",
            "SC_Fixture");

        Assert.Equal(0.75f, cue.VolumeMultiplier);
        Assert.Equal(1.125f, cue.PitchMultiplier);
        var mixer = Assert.IsType<USoundNodeMixer>(cue.FirstNode?.Load<USoundNode>());
        Assert.Equal(2, mixer.ChildNodes.Length);

        var players = mixer.ChildNodes
            .Select(reference => Assert.IsType<USoundNodeWavePlayer>(reference.Load<USoundNode>()))
            .OrderBy(player => player.SoundWave?.Name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["SW_Inline", "SW_Streaming"], players.Select(player => player.SoundWave!.Name).ToArray());
        Assert.False(players[0].GetOrDefault<bool>("bLooping"));
        Assert.True(players[1].GetOrDefault<bool>("bLooping"));
    }

    private static void AssertWave(
        DefaultFileProvider provider,
        string name,
        bool expectedLooping,
        int expectedSeconds)
    {
        var sound = LoadExport<USoundWave>(
            provider,
            $"CUE4ParseFixtures/Content/Fixtures/Audio/{name}.uasset",
            name);
        // UE 5.8 stream caching serializes both loading-behavior variants as streamed platform data.
        Assert.True(sound.bStreaming);
        Assert.NotNull(sound.RunningPlatformData);
        Assert.Equal(expectedLooping, sound.GetOrDefault<bool>("bLooping"));

        sound.Decode(shouldDecompress: true, out var format, out var decoded);
        Assert.Equal("WAV", format);
        var wave = ParsePcmWave(Assert.IsType<byte[]>(decoded));
        Assert.Equal(22_050, wave.SampleRate);
        Assert.Equal(1, wave.Channels);
        Assert.Equal(16, wave.BitsPerSample);
        Assert.Equal(expectedSeconds * wave.SampleRate, wave.SampleCount);

        var samples = wave.Samples;
        Assert.Contains(samples, sample => sample > 20_000);
        Assert.Contains(samples, sample => sample < -20_000);
        Assert.InRange(CountRisingZeroCrossings(samples[..(wave.SampleRate / 2)]), 215, 225);
    }

    private static PcmWave ParsePcmWave(byte[] data)
    {
        ReadOnlySpan<byte> bytes = data;
        Assert.True(bytes.Length >= 44);
        Assert.True(bytes[..4].SequenceEqual("RIFF"u8), "Decoded audio is not a RIFF container.");
        Assert.True(bytes.Slice(8, 4).SequenceEqual("WAVE"u8), "Decoded RIFF data is not WAVE audio.");

        ReadOnlySpan<byte> formatChunk = default;
        ReadOnlySpan<byte> sampleData = default;
        for (var offset = 12; offset + 8 <= bytes.Length;)
        {
            var chunkLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(offset + 4, 4));
            Assert.InRange(chunkLength, 0, bytes.Length - offset - 8);
            var chunk = bytes.Slice(offset + 8, chunkLength);
            if (bytes.Slice(offset, 4).SequenceEqual("fmt "u8))
                formatChunk = chunk;
            else if (bytes.Slice(offset, 4).SequenceEqual("data"u8))
                sampleData = chunk;
            offset += 8 + chunkLength + (chunkLength & 1);
        }

        Assert.True(formatChunk.Length >= 16);
        Assert.False(sampleData.IsEmpty);
        Assert.Equal((short) 1, BinaryPrimitives.ReadInt16LittleEndian(formatChunk));
        var channels = BinaryPrimitives.ReadInt16LittleEndian(formatChunk.Slice(2, 2));
        var sampleRate = BinaryPrimitives.ReadInt32LittleEndian(formatChunk.Slice(4, 4));
        var bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(formatChunk.Slice(14, 2));
        var dataLength = sampleData.Length;
        Assert.Equal(0, dataLength % sizeof(short));

        var samples = new short[dataLength / sizeof(short)];
        for (var index = 0; index < samples.Length; index++)
            samples[index] = BinaryPrimitives.ReadInt16LittleEndian(sampleData.Slice(index * sizeof(short), sizeof(short)));

        return new PcmWave(channels, sampleRate, bitsPerSample, samples);
    }

    private static int CountRisingZeroCrossings(ReadOnlySpan<short> samples)
    {
        var count = 0;
        for (var index = 1; index < samples.Length; index++)
        {
            if (samples[index - 1] <= 0 && samples[index] > 0)
                count++;
        }

        return count;
    }

    private readonly record struct PcmWave(short Channels, int SampleRate, short BitsPerSample, short[] Samples)
    {
        public int SampleCount => Samples.Length / Channels;
    }
}
