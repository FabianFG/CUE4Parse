﻿using System.Collections.Generic;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using FConfigSectionMap = System.Collections.Generic.Dictionary<CUE4Parse.UE4.Objects.UObject.FName, CUE4Parse.UE4.BinaryConfig.Objects.FConfigValue>;

namespace CUE4Parse.UE4.BinaryConfig.Objects;

public class FConfigSection
{
    public FConfigSectionMap ConfigSectionMap;
    public Dictionary<FName, string> ArrayOfStructKeys;

    public FConfigSection(FArchive Ar)
    {
        ConfigSectionMap = Ar.ReadMap(Ar.ReadFName, () => new FConfigValue(Ar));
        ArrayOfStructKeys = Ar.ReadMap(Ar.ReadFName, Ar.ReadFString);
    }
}
