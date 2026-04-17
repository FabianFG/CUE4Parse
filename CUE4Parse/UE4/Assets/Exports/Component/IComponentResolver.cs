using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public interface IComponentResolver
{
    public IEnumerable<UObject> GetExportableReferences();
}
