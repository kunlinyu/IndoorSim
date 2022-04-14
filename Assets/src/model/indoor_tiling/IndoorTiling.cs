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
    [JsonPropertyAttribute] private List<CellVertex> vertexPool = new List<CellVertex>();
    [JsonPropertyAttribute] private List<CellBoundary> boundaryPool = new List<CellBoundary>();
    [JsonPropertyAttribute] private List<CellSpace> spacePool = new List<CellSpace>();
    [JsonPropertyAttribute] private List<RepresentativeLine> rLinePool = new List<RepresentativeLine>();

    [JsonIgnore] private IDGenInterface IdGenVertex;
    [JsonIgnore] private IDGenInterface IdGenBoundary;
    [JsonIgnore] private IDGenInterface IdGenSpace;

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

    public IndoorTiling(IDGenInterface IdGenVertex, IDGenInterface IdGenBoundary, IDGenInterface IdGenSpace)
    {
        this.IdGenVertex = IdGenVertex;
        this.IdGenBoundary = IdGenBoundary;
        this.IdGenSpace = IdGenSpace;
    }

    public IndoorTiling(IndoorTiling another)
    {

        this.boundaryPool.AddRange(another.boundaryPool);
        this.spacePool.AddRange(another.spacePool);
        this.vertexPool.AddRange(another.vertexPool);
        this.rLinePool.AddRange(another.rLinePool);

        this.IdGenVertex = another.IdGenVertex.clone();
        this.IdGenBoundary = another.IdGenBoundary.clone();
        this.IdGenSpace = another.IdGenSpace.clone();

        foreach (var entry in another.vertex2Boundaries)
        {
            this.vertex2Boundaries[entry.Key] = new HashSet<CellBoundary>();
            this.vertex2Boundaries[entry.Key].UnionWith(entry.Value);
        }
        foreach (var entry in another.vertex2Spaces)
        {
            this.vertex2Spaces[entry.Key] = new HashSet<CellSpace>();
            this.vertex2Spaces[entry.Key].UnionWith(entry.Value);
        }
        foreach (var entry in another.space2RLines)
        {
            this.space2RLines[entry.Key] = new HashSet<RepresentativeLine>();
            this.space2RLines[entry.Key].UnionWith(entry.Value);
        }
        foreach (var entry in another.boundary2RLines)
        {
            this.boundary2RLines[entry.Key] = new HashSet<RepresentativeLine>();
            this.boundary2RLines[entry.Key].UnionWith(entry.Value);
        }
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, Coordinate endCoor)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { startCoor, endCoor });

        if (ls.NumPoints < 2) throw new ArgumentException("line string of boundary should have 2 points at least");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;

        var start = CellVertex.Instantiate(ls.StartPoint, IdGenVertex);
        AddVertexInternal(start);

        var end = CellVertex.Instantiate(ls.EndPoint, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary.Gen());
        AddBoundaryInternal(boundary);
        ConsistencyCheck();
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { start.Coordinate, endCoor });

        if (ls.NumPoints < 2) throw new ArgumentException("line string of boundary should have 2 points at least");
        if (!vertexPool.Contains(start)) throw new ArgumentException("can not find vertex start");
        if (start.Geom.Distance(ls.GetPointN(0)) > 1e-3) throw new ArgumentException("The first point of ling string should equal to coordinate of start");
        if (endCoor.Distance(ls.GetPointN(ls.NumPoints - 1).Coordinate) > 1e-3) throw new ArgumentException("The last point of ling string should equal to coordinate of end");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;

        var end = CellVertex.Instantiate(endCoor, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary.Gen());
        AddBoundaryInternal(boundary);
        ConsistencyCheck();
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { startCoor, end.Coordinate });

        if (ls.NumPoints < 2) throw new ArgumentException("line string of boundary should have 2 points at least");
        if (!vertexPool.Contains(end)) throw new ArgumentException("can not find vertex end");
        if (startCoor.Distance(ls.GetPointN(0).Coordinate) > 1e-3) throw new ArgumentException("The first point of ling string should equal to coordinate of start");
        if (end.Geom.Distance(ls.GetPointN(ls.NumPoints - 1)) > 1e-3) throw new ArgumentException("The last point of ling string should equal to coordinate of end");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;

        var start = CellVertex.Instantiate(startCoor, IdGenVertex);
        AddVertexInternal(start);

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary.Gen());
        AddBoundaryInternal(boundary);
        ConsistencyCheck();
        return boundary;
    }

    enum NewCellSpaceCase
    {
        NewCellSpace,  // This is a new cellspace.
        HoleOfAnother, // This is a hole of another cellspace. We should create one cellspace and add a hole to the "another" one.
        Split,         // Split cellspace to two. We should remove the old one and create two.
        SplitNeedReSearch,  // Like Split, but for the two new created cellspace, one surround another. The inner one have one point common point with another
                            // it may be define as a hole but re-search ring is easier.
    }
    public CellBoundary? AddBoundary(CellVertex start, CellVertex end)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { start.Coordinate, end.Coordinate });

        if (ls.NumPoints < 2) throw new ArgumentException("line string of boundary should have 2 points at least");
        if (!vertexPool.Contains(start)) throw new ArgumentException("can not find vertex start");
        if (!vertexPool.Contains(end)) throw new ArgumentException("can not find vertex end");
        if (start.Geom.Distance(ls.GetPointN(0)) > 1e-3) throw new ArgumentException("The first point of ling string should equal to coordinate of start");
        if (end.Geom.Distance(ls.GetPointN(ls.NumPoints - 1)) > 1e-3) throw new ArgumentException("The last point of ling string should equal to coordinate of end");
        if (System.Object.ReferenceEquals(start, end)) throw new ArgumentException("should not connect same vertex");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;
        if (VertexPair2Boundaries(start, end).Count > 0) return null;  // don't support multiple boundary between two vertices yet

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary.Gen());

        // create new CellSpace
        List<JumpInfo> jumps1 = PSLGPolygonSearcher.Search(new JumpInfo() { target = start, through = boundary }, end, AdjacentFinder);
        List<JumpInfo> jumps2 = PSLGPolygonSearcher.Search(new JumpInfo() { target = end, through = boundary }, start, AdjacentFinder);
        List<JumpInfo> reJumps1 = PSLGPolygonSearcher.Search(new JumpInfo() { target = start, through = boundary }, end, AdjacentFinder, false);
        List<JumpInfo> reJumps2 = PSLGPolygonSearcher.Search(new JumpInfo() { target = end, through = boundary }, start, AdjacentFinder, false);

        // List<List<JumpInfo>> rings1 = PSLGPolygonSearcher.Jumps2Rings(jumps1, SplitRingType.SplitByRepeatedVertex);

        var ring1 = jumps1.Select(ji => ji.target.Coordinate).ToList();
        ring1.Add(start.Coordinate);
        var ring2 = jumps2.Select(ji => ji.target.Coordinate).ToList();
        ring2.Add(end.Coordinate);

        // Add Boundary
        AddBoundaryInternal(boundary);

        // can not reach
        if (ring1.Count < 2 && ring2.Count < 2) return boundary;

        var gf = new GeometryFactory();
        bool path1IsCCW = gf.CreateLinearRing(ring1.ToArray()).IsCCW;
        bool path2IsCCW = gf.CreateLinearRing(ring2.ToArray()).IsCCW;

        CellSpace cellSpace1 = CreateCellSpaceInternal(jumps1);
        CellSpace cellSpace2 = CreateCellSpaceInternal(jumps2);
        CellSpace? oldCellSpace = spacePool.FirstOrDefault(cs => cs.Geom.Contains(MiddlePoint(ls)));

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
                    AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps1));
                else
                    AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps2));
                break;

            case NewCellSpaceCase.Split:
                RemoveSpaceInternal(oldCellSpace!);
                AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps1));
                AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps2));
                break;

            case NewCellSpaceCase.SplitNeedReSearch:
                RemoveSpaceInternal(oldCellSpace!);
                AddSpaceConsiderHole(CreateCellSpaceWithHole(reJumps1));
                AddSpaceConsiderHole(CreateCellSpaceWithHole(reJumps2));
                break;

            case NewCellSpaceCase.HoleOfAnother:
                if (path1IsCCW)
                    AddSpaceConsiderHole(cellSpace1);
                else
                    AddSpaceConsiderHole(cellSpace2);
                break;
        }
        // ConsistencyCheck();
        return boundary;
    }

    public CellVertex SplitBoundary(CellBoundary boundary, Coordinate middleCoor)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("unknown boundary");
        if (boundary.Geom.NumPoints > 2) throw new ArgumentException("We don't support split boundary with point more than 2 yet");

        // Create vertex
        CellVertex middleVertex = CellVertex.Instantiate(middleCoor, IdGenVertex);
        AddVertexInternal(middleVertex);

        // Remove old boundary
        RemoveBoundaryInternal(boundary);

        // Create and add new boundary
        CellBoundary newBoundary1 = new CellBoundary(boundary.P0, middleVertex, IdGenBoundary.Gen());
        CellBoundary newBoundary2 = new CellBoundary(middleVertex, boundary.P1, IdGenBoundary.Gen());
        AddBoundaryInternal(newBoundary1);
        AddBoundaryInternal(newBoundary2);

        // update space and vertex2space indices
        List<CellSpace> spaces = Boundary2Space(boundary);
        foreach (var space in spaces)
            space.SplitBoundary(boundary, newBoundary1, newBoundary2, middleVertex);
        vertex2Spaces[middleVertex] = new HashSet<CellSpace>(spaces);

        return middleVertex;
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
                    boundaries.UnionWith(vertex2Boundaries[vertex]);
                if (vertex2Spaces.ContainsKey(vertex))
                    spaces.UnionWith(vertex2Spaces[vertex]);
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

        foreach (var s in spaces)
        {
            s.UpdateFromVertex();
            s.OnUpdate?.Invoke();
        }

        foreach (var s1 in spaces)
        {
            foreach (var s2 in spacePool)
                if (!System.Object.ReferenceEquals(s1, s2))
                    if (s1.Geom.Relate(s2.Geom, "T********"))  // TODO magic string
                    {
                        valid = false;
                        goto validresult;
                    }

        }
        foreach (var s in spaces)
        {
            foreach (var hole in s.Holes)
                if (!s.ShellCellSpace().Geom.Contains(hole.Geom.Shell))
                {
                    valid = false;
                    goto validresult;
                }
        }

    validresult:

        if (valid)
        {
            vertices.ForEach(v => v.OnUpdate?.Invoke());
        }
        else
        {
            for (int i = 0; i < vertices.Count; i++)
                vertices[i].UpdateCoordinate(oldCoors[i]);
            foreach (var b in boundaries)
                b.UpdateFromVertex();
            foreach (var s in spaces)
            {
                s.UpdateFromVertex();
                s.OnUpdate?.Invoke();
            }
        }
    }

    public void RemoveBoundary(CellBoundary boundary)
    {
        RemoveBoundaryInternal(boundary);

        // Remove Vertex if no boundary connect to it
        if (vertex2Boundaries[boundary.P0].Count == 0)
            RemoveVertexInternal(boundary.P0);
        if (vertex2Boundaries[boundary.P1].Count == 0)
            RemoveVertexInternal(boundary.P1);

        // Remove space
        List<CellSpace> spaces = Boundary2Space(boundary);
        if (spaces.Count == 0)  // no cellspace related
        {
            // nothing
        }
        else if (spaces.Count == 1)  // only 1 cellspace related. Remove the cellspace.
        {
            RemoveSpaceInternal(spaces[0]);
        }
        else if (spaces[0].ShellCellSpace().Geom.Contains(spaces[1].ShellCellSpace().Geom) ||
                 spaces[1].ShellCellSpace().Geom.Contains(spaces[0].ShellCellSpace().Geom))  // one in the hole of another
        {
            CellSpace parent, child;
            if (spaces[0].ShellCellSpace().Geom.Contains(spaces[1].ShellCellSpace().Geom))
            {
                parent = spaces[0];
                child = spaces[1];
            }
            else
            {
                parent = spaces[1];
                child = spaces[0];
            }

            CellSpace? hole = parent.FindHole(child);
            if (hole != null)
            {
                parent.RemoveHole(child);
                RelateVertexSpace(parent);
                RemoveSpaceInternal(child);
            }
            else
            {
                List<JumpInfo> path = PSLGPolygonSearcher.Search(new JumpInfo() { target = boundary.P0, through = boundary }, boundary.P0, AdjacentFinder);
                List<CellSpace> holes = CreateCellSpaceMulti(path);
                parent.RemoveHole(child);
                foreach (var newHole in holes)
                    parent.AddHole(newHole);
                RelateVertexSpace(parent);

                RemoveSpaceInternal(child);
            }
        }
        else  // Two parallel cellspace. merge them
        {
            List<JumpInfo> path = PSLGPolygonSearcher.Search(new JumpInfo() { target = boundary.P0, through = boundary }, boundary.P0, AdjacentFinder);

            RemoveSpaceInternal(spaces[0]);
            RemoveSpaceInternal(spaces[1]);
            AddSpaceConsiderHole(CreateCellSpaceWithHole(path));
        }

    }

    public void AddRepresentativeLine(LineString ls, CellBoundary from, CellBoundary to, CellSpace through)
    {
        // new RepresentativeLine(ls, from, to, through);
    }

    public void RemoveRepresentativeLine(RepresentativeLine rLine)
    {

    }

    public ICollection<CellBoundary> VertexPair2Boundaries(CellVertex cv1, CellVertex cv2)
        => vertex2Boundaries[cv1].Where(b => System.Object.ReferenceEquals(b.Another(cv1), cv2)).ToList();
    private List<JumpInfo> AdjacentFinder(CellVertex cv)
    {
        if (vertex2Boundaries.ContainsKey(cv))
            return vertex2Boundaries[cv].Select(b => new JumpInfo() { target = b.Another(cv), through = b }).ToList();
        else
        {
            Debug.LogError(cv.Id);
            foreach (var entry in vertex2Boundaries)
                Debug.LogError(entry.Key.Id);
            throw new Exception(cv.Id);
        }
    }

    private List<CellSpace> Boundary2Space(CellBoundary boundary)
    {
        HashSet<CellSpace> potentialSpaces = new HashSet<CellSpace>();
        if (vertex2Spaces.ContainsKey(boundary.P0))
            potentialSpaces.UnionWith(vertex2Spaces[boundary.P0]);
        if (vertex2Spaces.ContainsKey(boundary.P1))
            potentialSpaces.UnionWith(vertex2Spaces[boundary.P1]);

        List<CellSpace> result = potentialSpaces.Where(space => space.allBoundaries.Contains(boundary)).ToList();
        if (result.Count > 2)
        {
            string debug = "GEOMETRYCOLLECTION(";
            foreach (var space in result) debug += space.ToString() + ", ";
            debug += ")";
            Debug.Log(debug);
            throw new InvalidOperationException("The boundary have more than one related spaces");
        }
        return result;
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

    private void AddVertexInternal(CellVertex vertex)
    {
        vertexPool.Add(vertex);
        vertex2Boundaries[vertex] = new HashSet<CellBoundary>();
        vertex2Spaces[vertex] = new HashSet<CellSpace>();
        OnVertexCreated?.Invoke(vertex);
    }

    private void RemoveVertexInternal(CellVertex vertex)
    {
        vertexPool.Remove(vertex);
        vertex2Boundaries.Remove(vertex);
        vertex2Spaces.Remove(vertex);
        OnVertexRemoved?.Invoke(vertex);
    }

    private void AddBoundaryInternal(CellBoundary boundary)
    {
        if (boundaryPool.Contains(boundary)) throw new ArgumentException("add redundant cell boundary");

        boundaryPool.Add(boundary);

        vertex2Boundaries[boundary.P0].Add(boundary);
        vertex2Boundaries[boundary.P1].Add(boundary);

        OnBoundaryCreated.Invoke(boundary);
    }

    private void RemoveBoundaryInternal(CellBoundary boundary)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("can not find cell boundary");

        // Remove Boundary only
        boundaryPool.Remove(boundary);

        // update lookup tables
        vertex2Boundaries[boundary.P0].Remove(boundary);
        vertex2Boundaries[boundary.P1].Remove(boundary);

        OnBoundaryRemoved?.Invoke(boundary);
    }

    private void AddSpaceInternal(CellSpace space)
    {
        if (spacePool.Contains(space)) throw new ArgumentException("add redundant space");
        space.Id = IdGenSpace.Gen();
        spacePool.Add(space);
        RelateVertexSpace(space);
        OnSpaceCreated?.Invoke(space);
    }

    private void RemoveSpaceInternal(CellSpace space)
    {
        if (!spacePool.Contains(space)) throw new ArgumentException("Can not find the space");
        spacePool.Remove(space);
        foreach (var vertex in space.allVertices)
            vertex2Spaces[vertex].Remove(space);
        OnSpaceRemoved?.Invoke(space);
    }

    private void RelateVertexSpace(CellSpace space)
    {
        var allVertices = space.allVertices;
        foreach (var entry in vertex2Spaces)
            if (entry.Value.Contains(space) && !allVertices.Contains(entry.Key))
                vertex2Spaces[entry.Key].Remove(space);
        foreach (var vertex in allVertices)
            vertex2Spaces[vertex].Add(space);
    }

    private void AddSpaceConsiderHole(CellSpace current)
    {
        CellSpace? spaceContainCurrent = null;
        List<CellSpace> holeOfCurrent = new List<CellSpace>();

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
            current.AddHole(hole);

        AddSpaceInternal(current);
    }

    private List<CellSpace> CreateCellSpaceMulti(List<JumpInfo> path)
    {
        List<List<JumpInfo>> rings = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedVertex);

        return rings.Select(ring => CreateCellSpaceInternal(ring)).ToList();
    }

    private CellSpace CreateCellSpaceWithHole(List<JumpInfo> path)
    {
        List<List<JumpInfo>> rings = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedBoundary);

        List<CellSpace> cellSpaces = rings.Select(ring => CreateCellSpaceInternal(ring)).ToList();

        double area = 0.0f;
        CellSpace shell = cellSpaces.First();
        foreach (var cellspace in cellSpaces)
            if (cellspace.Geom.Area > area)
            {
                area = cellspace.Geom.Area;
                shell = cellspace;
            }
        foreach (var cellspace in cellSpaces)
            if (cellspace != shell)
                shell.AddHole(cellspace);
        return shell;
    }
    private CellSpace CreateCellSpaceInternal(List<JumpInfo> jumps, bool CCW = true)
    {
        List<CellVertex> vertices = jumps.Select(ji => ji.target).ToList();
        List<CellBoundary> boundaries = jumps.Select(ji => ji.through).ToList();

        // should reverse or not
        List<Coordinate> polygonPoints = new List<Coordinate>();
        for (int i = 0; i < jumps.Count; i++)
        {
            LineString boundaryPoints = jumps[i].Geom;
            var ignoreLastOne = new ArraySegment<Coordinate>(boundaryPoints.Coordinates, 0, boundaryPoints.NumPoints - 1).ToArray();
            polygonPoints.AddRange(ignoreLastOne);
        }
        polygonPoints.Add(jumps[0].Geom.StartPoint.Coordinate);
        bool isCCW = new GeometryFactory().CreateLinearRing(polygonPoints.ToArray()).IsCCW;
        if (isCCW != CCW)
        {
            vertices.Reverse();
            boundaries.Reverse();
        }

        return new CellSpace(vertices, boundaries);
    }

    private string Digest()
    {
        string result = "";
        spacePool.Sort((space1, space2) => Math.Sign(space1.Geom.Area - space2.Geom.Area));
        foreach (var cellspace in spacePool)
            result += cellspace.Digest() + ",\n";
        return result;
    }


    [JsonIgnore] private static bool consistencyChecking = false;
    private void ConsistencyCheck()
    {
        if (consistencyChecking) return;
        consistencyChecking = true;

        string before = Digest();

        List<CellBoundary> boundaries = new List<CellBoundary>(boundaryPool);
        bool valid = true;
        IndoorTiling? tempIndoorTiling = null;
        foreach (var boundary in boundaries)
        {
            tempIndoorTiling = new IndoorTiling(this);

            Debug.Log("try remove " + boundary.Id);
            tempIndoorTiling.RemoveBoundary(boundary);

            Debug.Log("try add back");
            if (tempIndoorTiling.vertexPool.Contains(boundary.P0))
            {
                if (tempIndoorTiling.vertexPool.Contains(boundary.P1))
                    tempIndoorTiling.AddBoundary(boundary.P0, boundary.P1);
                else
                    tempIndoorTiling.AddBoundary(boundary.P0, boundary.P1.Coordinate);
            }
            else
            {
                if (tempIndoorTiling.vertexPool.Contains(boundary.P1))
                    tempIndoorTiling.AddBoundary(boundary.P0.Coordinate, boundary.P1);
                else
                    tempIndoorTiling.AddBoundary(boundary.P0.Coordinate, boundary.P1.Coordinate);
            }

            string after = tempIndoorTiling.Digest();
            if (before != after)
            {
                Debug.LogError(boundary.Id);
                Debug.LogError(boundary.Geom);
                Debug.LogError(before);
                Debug.LogError(after);
                valid = false;
            }
            tempIndoorTiling = null;
        }

        if (valid)
            Debug.Log($"ConsistencyCheck OK after try remove {boundaries.Count} boundaries");

        consistencyChecking = false;
    }




}
