using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Niagara;

public class FNiagaraDataInterfaceGeneratedFunction
{
    /** Name of the function as defined by the data interface. */
    public FName DefinitionName;

    /** Name of the instance. Derived from the definition name but made unique for this DI instance and specifier values. */
    public string InstanceName;

    /** Specifier values for this instance. */
    public (FName, FName)[] Specifiers;

    public FNiagaraVariableCommonReference[] VariadicInputs;
    public FNiagaraVariableCommonReference[] VariadicOutputs;

    public FNiagaraDataInterfaceGeneratedFunction(FAssetArchive Ar)
    {
        DefinitionName = Ar.ReadFName();
        InstanceName = Ar.ReadFString();
        Specifiers = Ar.ReadArray(() => (Ar.ReadFName(), Ar.ReadFName()));

        if (FNiagaraCustomVersion.Get(Ar) >= FNiagaraCustomVersion.Type.AddVariadicParametersToGPUFunctionInfo)
        {
            VariadicInputs = Ar.ReadArray(() => new FNiagaraVariableCommonReference(Ar));
            VariadicOutputs = Ar.ReadArray(() => new FNiagaraVariableCommonReference(Ar));
        }
    }
}

public class FNiagaraVariableCommonReference(FAssetArchive Ar) : FStructFallback(Ar, "NiagaraVariableCommonReference") { }
