using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable
public class Container
{
    [JsonPropertyAttribute] public string containerId { get; set; }
    [JsonPropertyAttribute] public Geometry? Geom { get; protected set; }
    [JsonPropertyAttribute] public Navigable navigable = Navigable.Navigable;
    [JsonPropertyAttribute] public List<Container> children = new List<Container>();
    [JsonPropertyAttribute] public Dictionary<string, string> kvp = new Dictionary<string, string>();

    public Container(string id) { this.containerId = id; }

    public bool Contains(string id) => Find(id) != null;

    public List<Container> AllNodeInContainerTree()
    {
        List<Container> result = new List<Container>() { this };
        children.ForEach(child => result.AddRange(child.AllNodeInContainerTree()));
        return result;
    }

    public Container? Find(string id)
    {
        if (containerId == id) return this;

        foreach (var son in children)
        {
            var result = son.Find(id);
            if (result != null) return result;
        }

        return null;
    }
}
