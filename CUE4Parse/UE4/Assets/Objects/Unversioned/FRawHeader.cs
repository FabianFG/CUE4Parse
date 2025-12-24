using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.MappingsProvider;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned;

[Flags]
public enum ERawHeaderFlags : uint
{
    None = 0,
    Reverse = 1 << 0, // reverse order after properties filtering
    RawProperties = 1 << 1, // all properties are raw no header/tag
    RawPropertiesExceptStructs = 1 << 2 | RawProperties, // Read StructProperty header
    SuperStructs = 1 << 3, // include super properties
    // TO-DO
    // SuperFirst = 1 << 4 | SuperStructs, // read super properties first
}

// Examples
// (0, -1) = all properties
// (k, n) = skip k, then read n properties
// (k, -3) = skip k, read to the end except the last 2 props
public class FRawHeader
{
    public readonly ERawHeaderFlags Flags;
    public readonly IReadOnlyList<(uint, int)> Fragments;
    // TO-DO Add reading by property names

    // TO-DO Maybe should be game specific, but for now can manually override it when needed
    public static FRawHeader FullRead = new([(0,-1)], ERawHeaderFlags.RawProperties | ERawHeaderFlags.SuperStructs);

    public FRawHeader(List<(uint, int)> fragments, ERawHeaderFlags flags = ERawHeaderFlags.None)
    {
        Flags = flags;
        Fragments = fragments;
    }

    public List<int> BuildIndices(Struct propMappings)
    {
        bool includeSuper = Flags.HasFlag(ERawHeaderFlags.SuperStructs);
        int totalCount = propMappings.CountProperties(includeSuper);
        var result = new List<int>(totalCount);

        uint current = 0;
        foreach (var (skip, count) in Fragments)
        {
            current += skip;
            if (current >= totalCount) break;
            var takeCount = count < 0 ? totalCount - current + count + 1 : count;
            takeCount = Math.Clamp(takeCount, 0, totalCount - current);
            result.AddRange(Enumerable.Range((int)current, (int)takeCount));
            current += (uint)takeCount;
        }

        if (Flags.HasFlag(ERawHeaderFlags.Reverse)) result.Reverse();
        return result;
    }
}
