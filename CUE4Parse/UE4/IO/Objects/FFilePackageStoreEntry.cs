using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FFilePackageStoreEntry
    {
        public int ExportCount;
        public int ExportBundleCount;
        public FPackageId[] ImportedPackages;
        public FSHAHash[] ShaderMapHashes;

        public FFilePackageStoreEntry(FArchive Ar, EIoContainerHeaderVersion version)
        {
            if (version >= EIoContainerHeaderVersion.Initial)
            {
                if (version < EIoContainerHeaderVersion.NoExportInfo)
                {
                    ExportCount = Ar.Read<int>();
                    ExportBundleCount = Ar.Read<int>();
                }

                ImportedPackages = ReadCArrayView<FPackageId>(Ar);
                ShaderMapHashes = ReadCArrayView(Ar, () => new FSHAHash(Ar));
            }
            else
            {
                Ar.Position += 8; // ExportBundlesSize
                ExportCount = Ar.Read<int>();
                ExportBundleCount = Ar.Read<int>();
                Ar.Position += 8; // LoadOrder + Pad
                ImportedPackages = ReadCArrayView<FPackageId>(Ar);
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
