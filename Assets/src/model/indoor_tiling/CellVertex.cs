using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

public class CellVertex
{
    [JsonPropertyAttribute] public Point Geom { get; private set; }
    [JsonPropertyAttribute] public int Id { get; private set; } = 0;

    [JsonIgnore] public Coordinate Coordinate { get => Geom.Coordinate; }

    [JsonIgnore] public Action OnUpdate = () => {};

    public CellVertex(Point p, int id = 0)
    {
        Geom = p;
        Id = id;
    }

    public CellVertex(Coordinate p, int id = 0)
    {
        Geom = new GeometryFactory().CreatePoint(p);
        Id = id;
    }

}