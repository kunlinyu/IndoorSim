using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

using UnityEngine;

using JumpInfo = PSLGPolygonSearcher.JumpInfo;

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

    [JsonIgnore] public Action<CellVertex> OnVertexCreated = (v) => { };
    [JsonIgnore] public Action<CellBoundary> OnBoundaryCreated = (b) => { };
    [JsonIgnore] public Action<CellSpace> OnSpaceCreated = (s) => { };

    [JsonIgnore] public Action<CellVertex> OnVertexRemoved = (v) => { };
    [JsonIgnore] public Action<CellBoundary> OnBoundaryRemoved = (b) => { };
    [JsonIgnore] public Action<CellSpace> OnSpaceRemoved = (s) => { };

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

    public List<CellBoundary> VerticesPair2Boundary(CellVertex cv1, CellVertex cv2)
    {
        List<CellBoundary> result = new List<CellBoundary>();

        if (!vertex2Boundaries.ContainsKey(cv1) || !vertex2Boundaries.ContainsKey(cv2))
            return result;

        var b1s = vertex2Boundaries[cv1];
        var b2s = vertex2Boundaries[cv2];
        foreach (var b1 in b1s)
            foreach (var b2 in b2s)
                if (System.Object.ReferenceEquals(b1, b2))
                    result.Add(b2);
        return result;
    }

    public ICollection<CellVertex> Neighbor(CellVertex cv)
        => vertex2Boundaries[cv].Select(b => b.Another(cv)).ToList();

    public ICollection<CellBoundary> VertexPair2Boundaries(CellVertex cv1, CellVertex cv2)
        => vertex2Boundaries[cv1].Where(b => System.Object.ReferenceEquals(b.Another(cv1), cv2)).ToList();

    public IndoorTiling()
    {
    }

    public void AddBoundary(LineString ls)
        => AddBoundary(ls, new CellVertex(ls.StartPoint), new CellVertex(ls.EndPoint));

    private bool Reachable(CellVertex start, CellVertex end)
    {
        return true;  // TODO
    }

    private List<JumpInfo> AdjacentFinder(CellVertex cv)
    {
        var result = new List<JumpInfo>();

        HashSet<CellBoundary> boundaries = vertex2Boundaries[cv];
        foreach (CellBoundary boundary in boundaries)
            result.Add(new JumpInfo()
            {
                target = boundary.Another(cv),
                through = boundary
            });
        return result;
    }

    enum NewCellSpaceCase
    {
        NewCellSpace,  // This is a new cellspace.
        HoleOfAnother, // This is a hole of another cellspace. We should create one cellspace and add a hole to the "another" one.
        Split,         // Split cellspace to two. We should remove the old one and create two.
        SplitNeedReSearch,  // Like Split, but for the two new created cellspace, one surround another. The inner one have one point common point with another
                            // it may be define as a hole but re-search ring is easier.
    }

    public void AddBoundary(LineString ls, CellVertex start, CellVertex end)
    {
        if (ls.NumPoints < 2) throw new ArgumentException("line string of boundary should have 2 points at least");
        if (start.Geom.Distance(ls.GetPointN(0)) > 1e-3) throw new ArgumentException("The first point of ling string should equal to coordinate of start");
        if (end.Geom.Distance(ls.GetPointN(ls.NumPoints - 1)) > 1e-3) throw new ArgumentException("The last point of ling string should equal to coordinate of end");
        if (System.Object.ReferenceEquals(start, end)) throw new ArgumentException("should not connect same vertex");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return;

        bool newStart = !vertexPool.Contains(start);
        bool newEnd = !vertexPool.Contains(end);
        CellBoundary boundary = new CellBoundary(ls, start, end);

        // create new CellSpace
        if (!newStart && !newEnd)
        {
            if (VerticesPair2Boundary(start, end).Count > 0) return;  // don't support multiple boundary between two vertices yet


            JumpInfo initJump1 = new JumpInfo() { target = start, through = boundary };
            List<JumpInfo> jumps1 = PSLGPolygonSearcher.Search(initJump1, end, AdjacentFinder);

            JumpInfo initJump2 = new JumpInfo() { target = end, through = boundary };
            List<JumpInfo> jumps2 = PSLGPolygonSearcher.Search(initJump2, start, AdjacentFinder);

            List<JumpInfo> reJumps1 = PSLGPolygonSearcher.Search(initJump1, end, AdjacentFinder, false);
            List<JumpInfo> reJumps2 = PSLGPolygonSearcher.Search(initJump2, start, AdjacentFinder, false);

            var ring1 = jumps1.Select(ji => ji.target.Coordinate).ToList();
            ring1.Add(initJump1.target.Coordinate);
            var ring2 = jumps2.Select(ji => ji.target.Coordinate).ToList();
            ring2.Add(initJump2.target.Coordinate);

            // Add Vertices
            if (newStart)
            {
                vertexPool.Add(start);
                OnVertexCreated(start);
            }
            if (newEnd)
            {
                vertexPool.Add(end);
                OnVertexCreated(end);
            }

            // Add Boundary
            AddBoundaryInternal(boundary);

            // can not reach
            if (ring1.Count < 2 && ring2.Count < 2) return;

            var gf = new GeometryFactory();
            bool path1IsCCW = gf.CreateLinearRing(ring1.ToArray()).IsCCW;
            bool path2IsCCW = gf.CreateLinearRing(ring2.ToArray()).IsCCW;

            CellSpace cellSpace1 = CreateCellSpace(jumps1);
            CellSpace cellSpace2 = CreateCellSpace(jumps2);
            CellSpace? oldCellSpace = PickCellSpace(MiddlePoint(ls));

            NewCellSpaceCase ncsCase;
            if (oldCellSpace == null)
                ncsCase = NewCellSpaceCase.NewCellSpace;
            else if (path1IsCCW && path2IsCCW)
                ncsCase = NewCellSpaceCase.Split;
            else if (oldCellSpace.Geom.Shell.Touches(start.Geom) || oldCellSpace.Geom.Shell.Touches(end.Geom))
                ncsCase = NewCellSpaceCase.SplitNeedReSearch;
            else
                ncsCase = NewCellSpaceCase.HoleOfAnother;

            Debug.Log(ncsCase);

            switch (ncsCase)
            {
                case NewCellSpaceCase.NewCellSpace:
                    if (path1IsCCW)
                        AddSpaceConsiderHole(cellSpace1);
                    else
                        AddSpaceConsiderHole(cellSpace2);
                    break;

                case NewCellSpaceCase.Split:
                    RemoveSpaceInternal(oldCellSpace!);
                    AddSpaceConsiderHole(cellSpace1);
                    AddSpaceConsiderHole(cellSpace2);
                    break;

                case NewCellSpaceCase.SplitNeedReSearch:
                    RemoveSpaceInternal(oldCellSpace!);
                    AddSpaceConsiderHole(CreateCellSpace(reJumps1));
                    AddSpaceConsiderHole(CreateCellSpace(reJumps2));
                    break;

                case NewCellSpaceCase.HoleOfAnother:
                    if (path1IsCCW)
                        AddSpaceConsiderHole(cellSpace1);
                    else
                        AddSpaceConsiderHole(cellSpace2);
                    break;
            }
        }
        else
        {
            // Add Vertices
            if (newStart)
            {
                vertexPool.Add(start);
                OnVertexCreated(start);
            }
            if (newEnd)
            {
                vertexPool.Add(end);
                OnVertexCreated(end);
            }

            // Add Boundary
            AddBoundaryInternal(boundary);
        }

        // remove useless vertex
    }

    private Point MiddlePoint(LineString ls)
    {
        if (ls.NumPoints < 2)
            throw new ArgumentException("Empty LingString don't have middlePoint");
        else if (ls.NumPoints == 2)
            return new GeometryFactory().CreatePoint(new Coordinate((ls.StartPoint.X + ls.EndPoint.X) / 2.0f, (ls.StartPoint.Y + ls.EndPoint.Y) / 2.0f));
        else
            return ls.GetPointN(1);
    }
    private void AddBoundaryInternal(CellBoundary boundary)
    {
        boundaryPool.Add(boundary);

        if (!vertex2Boundaries.ContainsKey(boundary.P0))
            vertex2Boundaries[boundary.P0] = new HashSet<CellBoundary>();
        vertex2Boundaries[boundary.P0].Add(boundary);

        if (!vertex2Boundaries.ContainsKey(boundary.P1))
            vertex2Boundaries[boundary.P1] = new HashSet<CellBoundary>();
        vertex2Boundaries[boundary.P1].Add(boundary);

        OnBoundaryCreated.Invoke(boundary);
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
        RelateVertexSpace(space);
        OnSpaceCreated(space);
    }

    private void RelateVertexSpace(CellSpace space)
    {
        foreach (var vertex in space.allVertices)
        {
            if (!vertex2Spaces.ContainsKey(vertex))
                vertex2Spaces[vertex] = new HashSet<CellSpace>();
            vertex2Spaces[vertex].Add(space);
        }
    }

    private void AddSpaceConsiderHole(CellSpace current)
    {
        List<CellSpace> holeOfCurrent = new List<CellSpace>();
        CellSpace? spaceContainCurrent = null;

        GeometryFactory gf = new GeometryFactory();

        foreach (CellSpace space in spacePool)
        {
            if (space.Geom.Contains(current.Geom.Shell))
                if (spaceContainCurrent == null)
                    spaceContainCurrent = space;
                else
                    throw new InvalidOperationException("more than one space contain current space");
            if (current.Geom.Contains(space.Geom))
                holeOfCurrent.Add(space);
        }

        if (spaceContainCurrent != null)
        {
            spaceContainCurrent.AddHole(current);
            RelateVertexSpace(spaceContainCurrent);
        }

        foreach (CellSpace hole in holeOfCurrent)
        {
            current.AddHole(hole);
        }
        RelateVertexSpace(current);

        AddSpaceInternal(current);
    }

    private void RemoveSpaceInternal(CellSpace space)
    {
        if (!spacePool.Contains(space)) throw new ArgumentException("Can not find the space");
        spacePool.Remove(space);
        foreach (var vertex in space.shellVertices)
            vertex2Spaces[vertex].Remove(space);
        OnSpaceRemoved(space);
    }

    private CellSpace CreateCellSpace(List<JumpInfo> bbt)
    {
        List<Coordinate> polygonPoints = new List<Coordinate>();
        for (int i = 0; i < bbt.Count; i++)
        {
            LineString boundaryPoints = bbt[i].Geom;
            var ignoreLastOne = new ArraySegment<Coordinate>(boundaryPoints.Coordinates, 0, boundaryPoints.NumPoints - 1).ToArray();
            polygonPoints.AddRange(ignoreLastOne);
        }
        polygonPoints.Add(bbt[0].Geom.StartPoint.Coordinate);
        Polygon polygon = new GeometryFactory().CreatePolygon(polygonPoints.ToArray());

        List<CellVertex> vertices = bbt.Select(ji => ji.target).ToList();
        List<CellBoundary> boundaries = bbt.Select(ji => ji.through).ToList();

        if (!new GeometryFactory().CreateLinearRing(polygonPoints.ToArray()).IsCCW)
        {
            vertices.Reverse();
            boundaries.Reverse();
        }

        return new CellSpace(polygon, vertices, boundaries);

    }

    public void UpdateVertices(List<CellVertex> vertices, List<Coordinate> coors)
    {
        if (vertices.Count != coors.Count) throw new ArgumentException("vertices count should equals to coors count");
        List<Coordinate> oldCoors = vertices.Select(v => v.Coordinate).ToList();

        for (int i = 0; i < vertices.Count; i++)
            vertices[i].UpdateCoordinate(coors[i]);

        HashSet<CellBoundary> boundaries = new HashSet<CellBoundary>();
        HashSet<CellSpace> spaces = new HashSet<CellSpace>();
        foreach (var vertex in vertices)
            if (vertexPool.Contains(vertex))
            {
                if (vertex2Boundaries.ContainsKey(vertex))
                    foreach (var b in vertex2Boundaries[vertex])
                        boundaries.Add(b);
                if (vertex2Spaces.ContainsKey(vertex))
                    foreach (var s in vertex2Spaces[vertex])
                        spaces.Add(s);
            }
            else throw new ArgumentException("can not find vertex");
        Debug.Log("related boundaries: " + boundaries.Count);
        Debug.Log("related spaces    : " + spaces.Count);

        foreach (var b in boundaries)
            b.UpdateFromVertex();

        bool valid = true;
        foreach (var b1 in boundaries)
        {
            foreach (var b2 in boundaryPool)
                if (!System.Object.ReferenceEquals(b1, b2))
                    if (b1.Geom.Crosses(b2.Geom))
                    {
                        valid = false;
                        goto validresult;
                    }
        }
    validresult:

        if (valid)
        {
            vertices.ForEach(v => v.OnUpdate?.Invoke());
            foreach (var s in spaces)
                s.UpdateFromVertex();
            Debug.Log("valid");
        }
        else
        {
            Debug.Log("invalid");
            for (int i = 0; i < vertices.Count; i++)
                vertices[i].UpdateCoordinate(oldCoors[i]);
            foreach (var b in boundaries)
                b.UpdateFromVertex();
        }



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
