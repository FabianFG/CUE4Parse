using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FPackageStoreEntry
    {
        public ulong ExportBundlesSize;
        public int ExportCount;
        public int ExportBundleCount;
        public uint LoadOrder;
        public uint Pad;
        public FPackageId[] ImportedPackages;
        public FSHAHash[] ShaderMapHashes;

        public FPackageStoreEntry(FArchive Ar)
        {
            ExportBundlesSize = Ar.Read<ulong>();
            ExportCount = Ar.Read<int>();
            ExportBundleCount = Ar.Read<int>();
            LoadOrder = Ar.Read<uint>();
            Pad = Ar.Read<uint>();
            ImportedPackages = ReadCArrayView<FPackageId>(Ar);
            if (Ar.Game >= EGame.GAME_UE5_0)
            {
                ShaderMapHashes = ReadCArrayView(Ar, () => new FSHAHash(Ar));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T[] ReadCArrayView<T>(FArchive Ar) where T : struct
        {
            var initialPos = Ar.Position;
            var arrayNum = Ar.Read<int>();
            var offsetToDataFromThis = Ar.Read<int>();
            if (arrayNum == 0)
            {
                return Array.Empty<T>();
            }

            var continuePos = Ar.Position;
            Ar.Position = initialPos + offsetToDataFromThis;
            var result = Ar.ReadArray<T>(arrayNum);
            Ar.Position = continuePos;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T[] ReadCArrayView<T>(FArchive Ar, Func<T> getter)
        {
            var initialPos = Ar.Position;
            var arrayNum = Ar.Read<int>();
            var offsetToDataFromThis = Ar.Read<int>();
            if (arrayNum == 0)
            {
                return Array.Empty<T>();
            }

            var continuePos = Ar.Position;
            Ar.Position = initialPos + offsetToDataFromThis;
            var result = Ar.ReadArray(arrayNum, getter);
            Ar.Position = continuePos;
            return result;
        }
    }
}