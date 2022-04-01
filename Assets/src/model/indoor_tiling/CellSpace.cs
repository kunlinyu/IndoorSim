using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable
public class CellSpace
{
    [JsonPropertyAttribute] public Polygon Geom { get; private set; }
    [JsonPropertyAttribute] public List<CellVertex> Vertices { get; private set; }
    [JsonPropertyAttribute] public List<CellBoundary> Boundaries { get; private set; }
    [JsonPropertyAttribute] public bool Navigable { get; set; } = false;
    [JsonIgnore] public Action OnUpdate = () => { };

    public CellSpace(Polygon polygon, ICollection<CellVertex> vertices, ICollection<CellBoundary> boundaries)
    {
        Geom = polygon;
        Vertices = new List<CellVertex>(vertices);
        Boundaries = new List<CellBoundary>(boundaries);
    }

    public void AddHole(CellSpace cellSpace)
        => AddHole(cellSpace.Geom.Shell, cellSpace.Vertices, cellSpace.Boundaries);

    public void AddHole(LinearRing hole, List<CellVertex> vertices, List<CellBoundary> boundaries)
    {
        LinearRing shell = Geom.Shell;
        List<LinearRing> holes = new List<LinearRing>(Geom.Holes);
        holes.Add(hole);
        Polygon polygon = new GeometryFactory().CreatePolygon(shell, holes.ToArray());
        if (polygon.IsSimple)
        {
            Geom = polygon;
            Vertices.AddRange(vertices);
            Boundaries.AddRange(boundaries);
        }
        else
        {
            throw new ArgumentException("Can not add the hole to get a \"Simple\" polygon");
        }
    }

    public void Update()
    {
        // TODO: vertices to geom;
    }

}
