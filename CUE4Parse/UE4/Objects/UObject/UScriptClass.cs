using CUE4Parse.UE4.Assets;

namespace CUE4Parse.UE4.Objects.UObject;

// Not an engine class, this inherits the UClass engine class to keep things simple
[SkipObjectRegistration]
public class UScriptClass : UClass
{
    public string? FullTypeIdentifier { get; }

    public UScriptClass(string className, string? fullTypeIdentifier = null)
    {
        Name = className;
        FullTypeIdentifier = fullTypeIdentifier;
    }
}

[SkipObjectRegistration]
public class USharpClass(string className, string? fullTypeIdentifier = null) : UScriptClass(className, fullTypeIdentifier);

[SkipObjectRegistration]
public class UPythonClass(string className, string? fullTypeIdentifier = null) : UScriptClass(className, fullTypeIdentifier);

[SkipObjectRegistration] // AngelScript
public class UASClass(string className, string? fullTypeIdentifier = null) : UScriptClass(className, fullTypeIdentifier);
