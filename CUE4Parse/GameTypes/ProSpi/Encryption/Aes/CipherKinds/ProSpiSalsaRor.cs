using System.Buffers.Binary;
using System.Numerics;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private static void ProSpiSalsaRorXor(Span<byte> payload, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> state = stackalloc uint[16];
        Span<byte> block = stackalloc byte[ChaChaBlockSize];
        BuildProSpiSalsaRorState(state, trailer, spec);

        for (var offset = 0; offset < payload.Length; offset += ChaChaBlockSize)
        {
            ProSpiSalsaRorBlock(state, spec.Rounds, block);
            var blockSize = Math.Min(ChaChaBlockSize, payload.Length - offset);
            for (var i = 0; i < blockSize; i++)
                payload[offset + i] ^= block[i];

            if (++state[12] == 0)
                state[13]++;
        }
    }

    private static void BuildProSpiSalsaRorState(Span<uint> state, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        state[0] = 0x61707865;
        state[1] = 0x3120646E;
        state[2] = 0x79622D36;
        state[3] = 0x6B206574;

        var firstHalfOffset = spec.KeyLayout switch
        {
            KeyLayout.Swap16 or KeyLayout.Second16Repeated => 16,
            _ => 0
        };
        var secondHalfOffset = spec.KeyLayout switch
        {
            KeyLayout.First16Repeated => 0,
            KeyLayout.Second16Repeated => 16,
            KeyLayout.Swap16 => 0,
            _ => 16
        };

        for (var i = 0; i < 4; i++)
            state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(_aesKey.AsSpan(firstHalfOffset + i * sizeof(uint), sizeof(uint)));
        for (var i = 0; i < 4; i++)
            state[8 + i] = BinaryPrimitives.ReadUInt32LittleEndian(_aesKey.AsSpan(secondHalfOffset + i * sizeof(uint), sizeof(uint)));

        state[12] = 0;
        state[13] = 0;
        var trailerWordOffset = spec.NonceOffset / sizeof(uint);
        state[14] = ReadTrailerWord(trailer, trailerWordOffset);
        state[15] = ReadTrailerWord(trailer, trailerWordOffset + 1);
    }

    private static void ProSpiSalsaRorBlock(ReadOnlySpan<uint> state, int rounds, Span<byte> block)
    {
        Span<uint> working = stackalloc uint[16];
        ProSpiSalsaRorCore(state, rounds, working);

        for (var i = 0; i < working.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(block.Slice(i * sizeof(uint), sizeof(uint)), working[i] + state[i]);
    }

    private static void ProSpiSalsaRorCore(ReadOnlySpan<uint> state, int rounds, Span<uint> working)
    {
        var v159 = state[0];
        var v160 = state[1];
        var v265 = state[2];
        var v266 = state[3];
        var v162 = state[4];
        var v267 = state[5];
        var v268 = state[6];
        var v163 = state[7];
        var v164 = state[8];
        var v245 = state[9];
        var v269 = state[10];
        var v263 = state[11];
        var v165 = state[12];
        var v166 = state[13];
        var v167 = state[14];
        var v243 = state[15];
        var v264 = v159;

        for (var round = 0; round < rounds; round++)
        {
            var v171 = BitOperations.RotateRight((v162 + v264) ^ v165, 16);
            var v173 = BitOperations.RotateRight((v171 + v164) ^ v162, 20);
            var v174 = v173 + v162 + v264;
            var v289 = BitOperations.RotateRight(v174 ^ v171, 24);
            var v175 = v289 + v171 + v164;
            var v290 = BitOperations.RotateRight(v175 ^ v173, 25);
            var v176 = BitOperations.RotateRight((v267 + v160) ^ v166, 16);
            var v177 = v176 + v245;
            var v178 = BitOperations.RotateRight(v177 ^ v267, 20);
            var v179 = v178 + v267 + v160;
            v245 = BitOperations.RotateRight(v179 ^ v176, 24);
            var v180 = v245 + v177;
            var v181 = BitOperations.RotateRight(v180 ^ v178, 25);
            var v182 = BitOperations.RotateRight((v268 + v265) ^ v167, 16);
            var v183 = BitOperations.RotateRight((v182 + v269) ^ v268, 20);
            var v184 = v183 + v268 + v265;
            var v288 = BitOperations.RotateRight(v184 ^ v182, 24);
            var v185 = v288 + v182 + v269;
            var v186 = BitOperations.RotateRight(v185 ^ v183, 25);
            var v188 = v163 + v266;
            var v189 = BitOperations.RotateRight(v188 ^ v243, 16);
            var v190 = v189 + v263;
            var v191 = BitOperations.RotateRight(v190 ^ v163, 20);
            var v192 = v191 + v188;
            var v193 = BitOperations.RotateRight(v192 ^ v189, 24);
            var v194 = v193 + v190;
            var v195 = BitOperations.RotateRight(v194 ^ v191, 25);
            var v196 = v181 + v174;
            var v197 = BitOperations.RotateRight(v196 ^ v193, 16);
            var v198 = v197 + v185;
            var v199 = BitOperations.RotateRight(v198 ^ v181, 20);
            v264 = v199 + v196;
            v243 = BitOperations.RotateRight(v264 ^ v197, 24);
            v269 = v243 + v198;
            v267 = BitOperations.RotateRight(v269 ^ v199, 25);
            var v201 = v186 + v179;
            var v202 = BitOperations.RotateRight(v201 ^ v289, 16);
            var v203 = v202 + v194;
            var v204 = BitOperations.RotateRight(v203 ^ v186, 20);
            v160 = v204 + v201;
            var v205 = BitOperations.RotateRight(v160 ^ v202, 24);
            v263 = v205 + v203;
            v268 = BitOperations.RotateRight(v263 ^ v204, 25);
            var v206 = v195 + v184;
            var v207 = BitOperations.RotateRight(v206 ^ v245, 16);
            var v208 = v207 + v175;
            var v209 = BitOperations.RotateRight(v208 ^ v195, 20);
            v165 = v205;
            v265 = v209 + v206;
            v166 = BitOperations.RotateRight(v265 ^ v207, 24);
            v164 = v166 + v208;
            v163 = BitOperations.RotateRight(v164 ^ v209, 25);
            var v210 = v290 + v192;
            var v211 = BitOperations.RotateRight(v210 ^ v288, 16);
            var v212 = v211 + v180;
            var v213 = BitOperations.RotateRight(v212 ^ v290, 20);
            v266 = v213 + v210;
            v167 = BitOperations.RotateRight(v266 ^ v211, 24);
            v245 = v167 + v212;
            v162 = BitOperations.RotateRight(v245 ^ v213, 25);
        }

        working[0] = v264;
        working[1] = v160;
        working[2] = v265;
        working[3] = v266;
        working[4] = v162;
        working[5] = v267;
        working[6] = v268;
        working[7] = v163;
        working[8] = v164;
        working[9] = v245;
        working[10] = v269;
        working[11] = v263;
        working[12] = v165;
        working[13] = v166;
        working[14] = v167;
        working[15] = v243;
    }
}
