using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable
public class CellSpace
{
    [JsonPropertyAttribute] public Polygon Geom { get; private set; }
    [JsonPropertyAttribute] public LinkedList<CellVertex> Vertices { get; private set; }
    [JsonPropertyAttribute] public LinkedList<CellBoundary> Boundaries { get; private set; }
    [JsonPropertyAttribute] public bool Navigable { get; set; } = false;
    [JsonIgnore] public Action OnUpdate = () => {};

    public CellSpace(Polygon polygon, ICollection<CellVertex> vertices, ICollection<CellBoundary> boundaries)
    {
        Geom = polygon;
        Vertices = new LinkedList<CellVertex>(vertices);
        Boundaries = new LinkedList<CellBoundary>(boundaries);
    }

    public void AddNewHole(CellVertex start, CellVertex end)
    {
        GeometryFactory gf = new GeometryFactory();

        // create hole
        Coordinate[] cas = new Coordinate[] {start.Coordinate, end.Coordinate, start.Coordinate};
        var hole = gf.CreateLinearRing(cas);

        // add hole
        LinearRing[] holes = new LinearRing[Geom.Holes.Length + 1];
        Array.Copy(Geom.Holes, holes, Geom.Holes.Length);
        holes[holes.Length - 1] = hole;
        Geom = gf.CreatePolygon(Geom.Shell, holes);

        // add vertices
        Vertices.AddLast(start);
        Vertices.AddLast(end);
    }

    public void ConnectTwoBoundary(CellVertex v1, CellVertex v2)
    {
        // TODO
    }

    public Polygon CutNewCellSpace(CellVertex v1, CellVertex v2)
    {
        // TODO
        return new GeometryFactory().CreatePolygon();
    }

    public void Update()
    {
        // TODO: vertices to geom;
    }

    public void ExtendBoundary(CellVertex vertex, CellVertex newVertex)
    {
        LinkedListNode<CellVertex> firstNode = Vertices.Find(vertex);
        LinkedListNode<CellVertex> lastNode = Vertices.FindLast(vertex);
        if (firstNode == lastNode)
        {
            Vertices.AddAfter(firstNode, firstNode.Value);
            Vertices.AddAfter(firstNode, newVertex);
        }
        else
        {
            throw new System.Exception("unsupported yet");
        }
    }
}
