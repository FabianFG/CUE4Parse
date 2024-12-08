using CUE4Parse.UE4.Assets;

namespace CUE4Parse.UE4.Objects.UObject;

// Not an engine class, this inherits the UClass engine class to keep things simple
[SkipObjectRegistration]
public class UScriptClass : UClass
{
    public UScriptClass(string className)
    {
        Name = className;
    }
}

[SkipObjectRegistration]
public class USharpClass(string className) : UScriptClass(className);

[SkipObjectRegistration]
public class UPythonClass(string className) : UScriptClass(className);

