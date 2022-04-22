using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable
public class Container
{
    [JsonPropertyAttribute] public string Id { get; set; }
    [JsonPropertyAttribute] public Geometry? Geom { get; protected set; }
    [JsonPropertyAttribute] public List<Container> sons = new List<Container>();

    public Container(string id) { this.Id = id; }
}
