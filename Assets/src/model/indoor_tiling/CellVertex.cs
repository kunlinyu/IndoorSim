using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

public class CellVertex
{
    [JsonPropertyAttribute] public string Id { get; private set; }
    [JsonPropertyAttribute] public Point Geom { get; private set; }

    [JsonIgnore] public Coordinate Coordinate { get => Geom.Coordinate; }

    [JsonIgnore] public Action OnUpdate = () => { };

    static public CellVertex Instantiate(Point p, IDGenInterface? gen) => new CellVertex(p, gen?.Gen() ?? "no id");
    static public CellVertex Instantiate(Coordinate coor, IDGenInterface? gen) => new CellVertex(coor, gen?.Gen() ?? "no id");

    public CellVertex() { Id = ""; Geom = new Point(new Coordinate(0.0f, 0.0f)); }  // for deserialization

    private CellVertex(Point p, string id)
    {
        Geom = p;
        Id = id;
    }

    private CellVertex(Coordinate p, string id)
    {
        Geom = new GeometryFactory().CreatePoint(p);
        Id = id;
    }

    public void UpdateCoordinate(Coordinate coor)
    {
        Geom = new GeometryFactory().CreatePoint(coor);
        OnUpdate?.Invoke();
    }

    public void UpdateCoordinate(Point point)
    {
        Geom = point;
        OnUpdate?.Invoke();
    }

}