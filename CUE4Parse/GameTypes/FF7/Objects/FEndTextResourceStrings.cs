using System.Collections.Generic;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FF7.Objects;

[JsonConverter(typeof(FEndTextResourceStringsConverter))]
public class FEndTextResourceStrings(FArchive Ar) : IUStruct
{
    public string Text = Ar.ReadFString();
    public Dictionary<FName, string> MetaData = Ar.ReadMap(Ar.ReadFName, Ar.ReadFString);
}
