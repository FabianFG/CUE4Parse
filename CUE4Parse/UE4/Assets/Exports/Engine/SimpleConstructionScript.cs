using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Engine;


public class USCS_Node : UObject 
{
    public USCS_Node[] GetChildNodes() 
    {
        return GetOrDefault<USCS_Node[]>("ChildNodes", []);
    }
    
    // Find a node by name in the current node and its children
    public USCS_Node? FindNode(string nodeName) 
    {
        var name = GetOrDefault<string>("VariableName");
        if (name == nodeName) 
        {
            return this;
        }

        foreach (var node in GetChildNodes()) 
        {
            var foundNode = node.FindNode(nodeName);
            if (foundNode != null) {
                return foundNode;
            }
        }

        return null;
    }

    public USceneComponent? GetComponentTemplate() 
    {
        return GetComponentTemplateAsResolvedObject()?.Load<USceneComponent>();
    }
    
    public ResolvedObject? GetComponentTemplateAsResolvedObject() 
    {
        return GetComponentTemplateAsIndex().ResolvedObject;
    }
    
    public FPackageIndex GetComponentTemplateAsIndex() 
    {
        return GetOrDefault("ComponentTemplate", new FPackageIndex());
    }
}

public class USimpleConstructionScript : UObject 
{
    public USCS_Node[] GetRootNodes() 
    {
        return GetOrDefault<USCS_Node[]>("RootNodes", []);
    }
    
    public USCS_Node[] GetAllNodesRecursive() 
    {
        var res = new List<USCS_Node>();
        
        void AddChildren(USCS_Node node) {
            res.Add(node);
            foreach (var child in node.GetOrDefault<USCS_Node[]>("ChildNodes")) {
                AddChildren(child);
            }
        }
        
        foreach (var node in GetRootNodes()) 
        {
            AddChildren(node);
        }

        return res.ToArray();
    }

    public USCS_Node? FindNode(string nodeName) 
    {
        // DefaultSceneRootNode
        if (nodeName == "DefaultSceneRoot" && TryGetValue(out USCS_Node? defaultSceneRootNode, "DefaultSceneRootNode")) {
            return defaultSceneRootNode;
        }
        var rootNodes = GetRootNodes();
        foreach (var node in rootNodes) {
            if (node.FindNode(nodeName) is { } foundNode) {
                return foundNode;
            }
        }

        return null;
    }
}