using System.Reflection;
using CUE4Parse.UE4.Assets.Exports;
using Xunit.Abstractions;

namespace CUE4Parse.Tests;

public class DuplicateClassesTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    public DuplicateClassesTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void DuplicateClasses()
    {
        var classes = new List<string>();
        var duplicates = new List<string>();

        //get all assemblies referenced by the entry assembly
        // var assemblies = AppDomain.CurrentDomain.GetAssemblies(); // includes current assembly too
        var assemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(Assembly.Load);
        assemblies = assemblies.Append(Assembly.GetExecutingAssembly());
        Type propertyHolderType = typeof(IPropertyHolder);

        foreach (var assembly in assemblies) {
            foreach (var definedType in assembly.DefinedTypes)
            {
                if (definedType.IsAbstract ||
                    definedType.IsInterface ||
                    !propertyHolderType.IsAssignableFrom(definedType))
                {
                    continue;
                }

                var name = definedType.Name;
                if ((name[0] == 'U' || name[0] == 'A') && char.IsUpper(name[1]))
                    name = name[1..];

                if (!classes.Contains(name))
                    classes.Add(name);
                else
                    duplicates.Add(name);
            }    
        }

        Assert.Empty(duplicates);
    }
}