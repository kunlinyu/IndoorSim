using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable
public class Container
{
    [JsonPropertyAttribute] public string containerId { get; set; }
    [JsonPropertyAttribute] public Geometry? Geom { get; protected set; }
    [JsonPropertyAttribute] public List<Container> children = new List<Container>();
    [JsonPropertyAttribute] public Dictionary<string, string> kvp = new Dictionary<string, string>();

    public Container(string id) { this.containerId = id; }
}
