using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

public class IndoorTiling
{
    public const double RemainSpaceSize = 10000.0d;
    [JsonPropertyAttribute] private ICollection<CellVertex> vertexPool = new List<CellVertex>();
    [JsonPropertyAttribute] private ICollection<CellBoundary> boundaryPool = new List<CellBoundary>();
    [JsonPropertyAttribute] private ICollection<CellSpace> spacePool = new List<CellSpace>();
    [JsonPropertyAttribute] private ICollection<RepresentativeLine> rLinePool = new List<RepresentativeLine>();

    [JsonIgnore] private Dictionary<CellVertex, HashSet<CellBoundary>> vertex2Boundaries = new Dictionary<CellVertex, HashSet<CellBoundary>>();
    [JsonIgnore] private Dictionary<CellVertex, HashSet<CellSpace>> vertex2Spaces = new Dictionary<CellVertex, HashSet<CellSpace>>();
    [JsonIgnore] private Dictionary<CellSpace, HashSet<RepresentativeLine>> space2RLines = new Dictionary<CellSpace, HashSet<RepresentativeLine>>();
    [JsonIgnore] private Dictionary<CellBoundary, HashSet<RepresentativeLine>> boundary2RLines = new Dictionary<CellBoundary, HashSet<RepresentativeLine>>();

    public CellSpace? PickCellSpace(Point point)
        => spacePool.FirstOrDefault(cs => cs.Geom.Contains(point));

    public CellVertex? PickCellVertex(Point point, double radius)
    {
        double minDistance = Double.MaxValue;
        CellVertex? vertex = null;
        foreach (CellVertex cv in vertexPool)
        {
            double distance = cv.Geom.Distance(point);
            if (minDistance > distance)
            {
                minDistance = distance;
                vertex = cv;
            }
        }
        return minDistance < radius ? vertex : null;
    }

    public CellBoundary? PickCellBoundary(Point point, double radius)
    {
        double minDistance = Double.MaxValue;
        CellBoundary? boundary = null;
        foreach (CellBoundary cb in boundaryPool)
        {
            double distance = cb.Geom.Distance(point);
            if (minDistance > distance)
            {
                minDistance = distance;
                boundary = cb;
            }
        }
        return minDistance < radius ? boundary : null;
    }

    public ICollection<CellVertex> Neighbor(CellVertex cv)
        => vertex2Boundaries[cv].Select(b => b.Another(cv)).ToList();

    public ICollection<CellBoundary> VertexPair2Boundaries(CellVertex cv1, CellVertex cv2)
        => vertex2Boundaries[cv1].Where(b => Object.ReferenceEquals(b.Another(cv1), cv2)).ToList();

    public IndoorTiling()
    {
        Coordinate[] cas = {
            new Coordinate( RemainSpaceSize,  RemainSpaceSize),
            new Coordinate(-RemainSpaceSize,  RemainSpaceSize),
            new Coordinate(-RemainSpaceSize, -RemainSpaceSize),
            new Coordinate( RemainSpaceSize, -RemainSpaceSize),
            new Coordinate( RemainSpaceSize,  RemainSpaceSize),
        };
    }

    public void AddBoundary(LineString ls)
        => AddBoundary(ls, new CellVertex(ls.StartPoint), new CellVertex(ls.EndPoint));

    private bool Reachable(CellVertex start, CellVertex end)
    {
        return true;  // TODO
    }

    private bool AnyPolygonContainNewBoundary(LineString ls)
    {
        return false;  // TODO
    }

    private List<PSLGPolygonSearcher.OutInfo> AdjacentFinder(CellVertex cv)
    {
        var result = new List<PSLGPolygonSearcher.OutInfo>();

        HashSet<CellBoundary> boundaries = vertex2Boundaries[cv];
        foreach (CellBoundary boundary in boundaries)
            result.Add(new PSLGPolygonSearcher.OutInfo()
            {
                targetCellVertex = boundary.Another(cv),
                boundary = boundary
            });
        return result;
    }

    public void AddBoundary(LineString ls, CellVertex start, CellVertex end)
    {
        if (ls.NumPoints < 2) throw new ArgumentException("line string of boundary should have 2 points at least");
        if (start.Geom.Distance(ls.GetPointN(0)) > 1e-3) throw new ArgumentException("The first point of ling string should equal to coordinate of start");
        if (end.Geom.Distance(ls.GetPointN(ls.NumPoints - 1)) > 1e-3) throw new ArgumentException("The last point of ling string should equal to coordinate of end");
        if (Object.ReferenceEquals(start, end)) throw new ArgumentException("should not connect same vertex");

        // TODO: Check intersection

        bool newStart = !vertexPool.Contains(start);
        bool newEnd = !vertexPool.Contains(end);
        CellBoundary boundary = new CellBoundary(ls, start, end);

        // create new CellSpace
        if (!newStart && !newEnd && Reachable(start, end))
        {
            List<CellVertex> path1 = PSLGPolygonSearcher.Search(start, end, ls.GetPointN(1), AdjacentFinder, out List<CellBoundary> boundaries1);
            path1.Add(start);
            boundaries1.Add(boundary);

            List<CellVertex> path2 = PSLGPolygonSearcher.Search(end, start, ls.GetPointN(ls.NumPoints - 2), AdjacentFinder, out List<CellBoundary> boundaries2);
            path2.Add(end);
            boundaries2.Add(boundary);

            var gf = new GeometryFactory();

            bool path1IsCCW = gf.CreateLinearRing(path1.Select(cv => cv.Coordinate).ToArray()).IsCCW;
            bool path2IsCCW = gf.CreateLinearRing(path2.Select(cv => cv.Coordinate).ToArray()).IsCCW;

            // TODO new cellspace is a hole of another?

            // Add Vertices
            if (newStart) vertexPool.Add(start);
            if (newEnd) vertexPool.Add(end);

            // Add Boundary
            AddBoundaryInternal(boundary);

            // split one CellSpace to two CellSpaces
            if (path1IsCCW && path2IsCCW)
            {
                CellSpace oldCellSpace = PickCellSpace(ls.InteriorPoint) ?? throw new ArgumentException("Oops!");
                CellSpace newCellSpace1 = CreateCellSpace(path1, boundaries1, gf);
                CellSpace newCellSpace2 = CreateCellSpace(path2, boundaries2, gf);

                RemoveSpaceInternal(oldCellSpace);

                AddSpaceInternal(newCellSpace1);
                AddSpaceInternal(newCellSpace2);
            }
            // create new CellSpace
            else if (path1IsCCW ^ path2IsCCW)
            {
                CellSpace space;
                if (path1IsCCW)
                    space = CreateCellSpace(path1, boundaries1, gf);
                else
                    space = CreateCellSpace(path2, boundaries2, gf);

                // Add Space
                AddSpaceInternal(space);

            }
            else
                throw new Exception("should not get to here");
        }

        // remove useless vertex
    }

    private void AddBoundaryInternal(CellBoundary boundary)
    {
        boundaryPool.Add(boundary);

        if (vertex2Boundaries.ContainsKey(boundary.P0))
            vertex2Boundaries[boundary.P0].Add(boundary);
        else
            vertex2Boundaries[boundary.P0] = new HashSet<CellBoundary>();

        if (vertex2Boundaries.ContainsKey(boundary.P1))
            vertex2Boundaries[boundary.P1].Add(boundary);
        else
            vertex2Boundaries[boundary.P1] = new HashSet<CellBoundary>();
    }

    private void RemoveBoundaryInternal(CellBoundary boundary)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("Can not find the boundary");
        boundaryPool.Remove(boundary);
        vertex2Boundaries[boundary.P0].Remove(boundary);
        vertex2Boundaries[boundary.P1].Remove(boundary);
    }

    private void AddSpaceInternal(CellSpace space)
    {
        spacePool.Add(space);
        foreach (var vertex in space.Vertices)
            if (vertex2Spaces.ContainsKey(vertex))
                vertex2Spaces[vertex].Add(space);
            else
                vertex2Spaces[vertex] = new HashSet<CellSpace>();
    }

    private void RemoveSpaceInternal(CellSpace space)
    {
        if (!spacePool.Contains(space)) throw new ArgumentException("Can not find the space");
        spacePool.Remove(space);
        foreach (var vertex in space.Vertices)
            vertex2Spaces[vertex].Remove(space);
    }

    private CellSpace CreateCellSpace(List<CellVertex> path, List<CellBoundary> boundaries, GeometryFactory gf)
    {
        List<Coordinate> polygonPoints = new List<Coordinate>();
        for (int i = 0; i < boundaries.Count; i++)
        {
            LineString boundaryPoints = boundaries[i].GeomOrder(path[i], path[i + 1]);
            var ignoreLastOne = new ArraySegment<Coordinate>(boundaryPoints.Coordinates, 0, boundaryPoints.NumPoints - 1).ToArray();
            polygonPoints.AddRange(ignoreLastOne);
        }
        polygonPoints.Add(polygonPoints[0]);
        Polygon polygon = gf.CreatePolygon(polygonPoints.ToArray());

        return new CellSpace(polygon, path.GetRange(0, path.Count - 1), boundaries);
    }

    public void RemoveBoundary(CellBoundary boundary)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("can not find cell boundary");

        // Remove Boundary only
        RemoveBoundaryInternal(boundary);
        // Or remove polygon
        // Or merge polygon
        // Remove Vertex

        // update lookup tables
    }

    public void RemoveSpace(CellSpace cs)
    {
        if (!spacePool.Contains(cs)) throw new ArgumentException("can not find cell space");

        // Remove cellspace
        // Remove representativeLine

        // update lookup tables
    }

    public void AddRepresentativeLine(LineString ls, CellBoundary from, CellBoundary to, CellSpace through)
    {
        // new RepresentativeLine(ls, from, to, through);
    }

    public void RemoveRepresentativeLine(RepresentativeLine rLine)
    {

    }




}
