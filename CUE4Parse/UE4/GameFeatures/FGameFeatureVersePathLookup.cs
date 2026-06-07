using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.GameFeatures;

public class FGameFeatureVersePathLookup
{
    static uint VerseLookupBinaryMagic   = 0x50564647; // 'G','F','V','P'
    static uint VerseLookupBinaryVersion = 1;

    public Dictionary<string, FName> VersePathToGfpMap;
    public Dictionary<FName, FStructFallback> GfpInfoMap;

    public FGameFeatureVersePathLookup(FArchive Ar)
    {
        var Magic = Ar.Read<uint>();

        if (Magic != VerseLookupBinaryMagic)
            throw new InvalidDataException($"Invalid magic value, expected {VerseLookupBinaryMagic:04x} got {Magic:04x}");

        var Version = Ar.Read<uint>();

        if (Version != VerseLookupBinaryVersion)
            throw new NotSupportedException($"Unsupported Version {Version:04x}");

        VersePathToGfpMap = Ar.ReadMap(Ar.ReadFString, Ar.ReadFName);
        var proxyArchive = new FObjectAndNameAsStringProxyArchive(Ar);
        GfpInfoMap = Ar.ReadMap(Ar.ReadFName, () => new FStructFallback(proxyArchive));
    }
}