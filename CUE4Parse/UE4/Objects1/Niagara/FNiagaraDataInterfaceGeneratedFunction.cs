using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Niagara
{
    public class FNiagaraDataInterfaceGeneratedFunction
    {
        /** Name of the function as defined by the data interface. */
        public FName DefinitionName;

        /** Name of the instance. Derived from the definition name but made unique for this DI instance and specifier values. */
        public string InstanceName;

        /** Specifier values for this instance. */
        public (FName, FName)[] Specifiers;

        public FNiagaraDataInterfaceGeneratedFunction(FArchive Ar)
        {
            DefinitionName = Ar.ReadFName();
            InstanceName = Ar.ReadFString();
            Specifiers = Ar.ReadArray<(FName, FName)>();
        }
    }
}