using System;
using System.Linq;
using System.Collections.Generic;

using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;
using Newtonsoft.Json;

using UnityEngine;
using UnityEditor;

using JumpInfo = PSLGPolygonSearcher.JumpInfo;

#nullable enable

[Serializable]
public class IndoorTiling
{
    [JsonPropertyAttribute] public List<CellVertex> vertexPool = new List<CellVertex>();
    [JsonPropertyAttribute] public List<CellBoundary> boundaryPool = new List<CellBoundary>();
    [JsonPropertyAttribute] public List<CellSpace> spacePool = new List<CellSpace>();
    [JsonPropertyAttribute] public List<RepresentativeLine> rLinePool = new List<RepresentativeLine>();
    [JsonPropertyAttribute] public InstructionHistory instructionHistory = new InstructionHistory();
    [JsonPropertyAttribute] public string digestCache = "";

    [JsonIgnore] public IDGenInterface? IdGenVertex { get; private set; }
    [JsonIgnore] public IDGenInterface? IdGenBoundary { get; private set; }
    [JsonIgnore] public IDGenInterface? IdGenSpace { get; private set; }

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

    public ICollection<Geometry> Polygonizer()
    {
        var polygonizer = new Polygonizer();
        polygonizer.Add(boundaryPool.Select(b => (Geometry)b.Geom).ToList());
        return polygonizer.GetPolygons();
    }

    public IndoorTiling()
    { }
    public IndoorTiling(IDGenInterface IdGenVertex, IDGenInterface IdGenBoundary, IDGenInterface IdGenSpace)
    {
        this.IdGenVertex = IdGenVertex;
        this.IdGenBoundary = IdGenBoundary;
        this.IdGenSpace = IdGenSpace;
    }

    public IndoorTiling(IndoorTiling another)
    {
        // TODO: should we trigger OnCreate?
        this.vertexPool.AddRange(another.vertexPool);
        this.boundaryPool.AddRange(another.boundaryPool);
        this.spacePool.AddRange(another.spacePool);
        this.rLinePool.AddRange(another.rLinePool);

        this.IdGenVertex = another.IdGenVertex?.clone();
        this.IdGenBoundary = another.IdGenBoundary?.clone();
        this.IdGenSpace = another.IdGenSpace?.clone();

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

    public string Serialize()
    {
        digestCache = CalcDigest();
        JsonConvert.DefaultSettings = ()
            => new JsonSerializerSettings
            {
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };
        return JsonConvert.SerializeObject(this, new WKTConverter(), new CoorConverter());
    }

    public bool DeserializeInPlace(string json, bool historyOnly = false)
    {
        foreach (var v in vertexPool)
            OnVertexRemoved?.Invoke(v);
        vertexPool.Clear();
        foreach (var b in boundaryPool)
            OnBoundaryRemoved?.Invoke(b);
        boundaryPool.Clear();
        foreach (var s in spacePool)
            OnSpaceRemoved?.Invoke(s);
        spacePool.Clear();

        instructionHistory.Clear();


        IndoorTiling? indoorTiling = Deserialize(json, historyOnly);
        if (indoorTiling == null) return false;

        if (historyOnly)
        {
            instructionHistory = indoorTiling.instructionHistory;
            IdGenVertex?.Reset();
            IdGenBoundary?.Reset();
            IdGenSpace?.Reset();
        }
        else
        {
            vertexPool = indoorTiling.vertexPool;
            boundaryPool = indoorTiling.boundaryPool;
            spacePool = indoorTiling.spacePool;
            rLinePool = indoorTiling.rLinePool;
            instructionHistory = indoorTiling.instructionHistory;

            // TODO: r-line?

            UpdateIndices();

            foreach (var v in vertexPool)
                OnVertexCreated?.Invoke(v);
            foreach (var b in boundaryPool)
                OnBoundaryCreated?.Invoke(b);
            foreach (var s in spacePool)
                OnSpaceCreated?.Invoke(s);

            IdGenVertex?.Reset(vertexPool.Select(v => v.Id).ToList());
            IdGenBoundary?.Reset(boundaryPool.Select(b => b.Id).ToList());
            IdGenSpace?.Reset(spacePool.Select(s => s.Id).ToList());
        }

        return true;
    }

    public static IndoorTiling? Deserialize(string json, bool historyOnly = false)
    {
        IndoorTiling? indoorTiling = JsonConvert.DeserializeObject<IndoorTiling>(json, new WKTConverter(), new CoorConverter(), new StackConverter());
        if (indoorTiling != null && historyOnly)
        {
            indoorTiling.vertexPool.Clear();
            indoorTiling.boundaryPool.Clear();
            indoorTiling.spacePool.Clear();
            indoorTiling.rLinePool.Clear();
            indoorTiling.instructionHistory.Uuundo();
        }
        return indoorTiling;
    }

    public void UpdateIndices()
    {
        vertex2Boundaries.Clear();
        foreach (CellBoundary boundary in boundaryPool)
        {
            if (!vertex2Boundaries.ContainsKey(boundary.P0))
                vertex2Boundaries[boundary.P0] = new HashSet<CellBoundary>();
            vertex2Boundaries[boundary.P0].Add(boundary);
            if (!vertex2Boundaries.ContainsKey(boundary.P1))
                vertex2Boundaries[boundary.P1] = new HashSet<CellBoundary>();
            vertex2Boundaries[boundary.P1].Add(boundary);
        }

        vertex2Spaces.Clear();
        foreach (CellSpace space in spacePool)
        {
            foreach (CellVertex vertex in space.allVertices)
            {
                if (!vertex2Spaces.ContainsKey(vertex))
                    vertex2Spaces[vertex] = new HashSet<CellSpace>();
                vertex2Spaces[vertex].Add(space);
            }
        }

        spacePool.ForEach(space => space.allBoundaries.ForEach(b => b.PartialBound(space)));


        // TODO:
        // space2RLines
        // boundary2RLines
    }

    private CellVertex? FindVertexId(string id)
        => vertexPool.FirstOrDefault(vertex => vertex.Id == id);

    private CellBoundary? FindBoundaryId(string id)
        => boundaryPool.FirstOrDefault(boundary => boundary.Id == id);

    private CellVertex? FindVertexCoor(Point coor)
        => vertexPool.FirstOrDefault(vertex => vertex.Geom.Distance(coor) < 1e-4f);  // TODO: magic number

    private CellVertex? FindVertexCoor(Coordinate coor)
        => vertexPool.FirstOrDefault(vertex => vertex.Geom.Coordinate.Distance(coor) < 1e-4f);  // TODO: magic number

    private CellBoundary? FindBoundaryGeom(LineString ls)  // BUG: this function may throw exception when undo
    {
        CellVertex? start = FindVertexCoor(ls.StartPoint);
        if (start == null)
            throw new ArgumentException("can not find vertex as start point of line string: " + ls.StartPoint.Coordinate);
        CellVertex? end = FindVertexCoor(ls.EndPoint);
        if (end == null)
            throw new ArgumentException("can not find vertex as end point of line string: " + ls.EndPoint.Coordinate);
        var boundaries = VertexPair2Boundaries(start, end);
        return boundaries.FirstOrDefault(b => b.Geom.Distance(MiddlePoint(ls)) < 1e-4);  // TODO: magic number
    }

    public bool Undo()
    {
        var instructions = instructionHistory.Undo();
        if (instructions.Count > 0)
        {
            InterpretInstruction(ReducedInstruction.Reverse(instructions));
            return true;
        }
        else
        {
            Debug.LogWarning("can not undo");
            return false;
        }
    }

    public bool Redo()
    {
        var instructions = instructionHistory.Redo();
        if (instructions.Count > 0)
        {
            InterpretInstruction(instructions);
            return true;
        }
        else
        {
            Debug.LogWarning("can not redo");
            return false;
        }
    }

    // TODO: consider id generator when interpret reverse instruction
    private void InterpretInstruction(List<ReducedInstruction> instructions)
    {
        foreach (var instruction in instructions)
            InterpretInstruction(instruction);
    }

    // TODO: consider id generator when interpret reverse instruction
    private void InterpretInstruction(ReducedInstruction instruction)
    {
        Debug.Log("interpret instruction: " + instruction);
        instructionHistory.IgnoreDo = true;
        switch (instruction.subject)
        {
            case SubjectType.Vertices:
                switch (instruction.predicate)
                {
                    case Predicate.Update:
                        {
                            List<CellVertex> vertices = new List<CellVertex>();
                            foreach (var coor in instruction.param.oldCoors)
                            {
                                CellVertex? vertex = FindVertexCoor(coor);
                                if (vertex != null)
                                    vertices.Add(vertex);
                                else
                                    throw new ArgumentException("one of vertex can not found: " + coor);
                            }
                            UpdateVertices(vertices, instruction.param.newCoors);
                            break;
                        }
                    default:
                        throw new ArgumentException("Unknown predicate of subject type Vertices: " + instruction.predicate);
                }
                break;
            case SubjectType.Boundary:
                switch (instruction.predicate)
                {
                    case Predicate.Add:
                        {
                            Coordinate startCoor = instruction.param.newLineString.StartPoint.Coordinate;
                            CellVertex? start = FindVertexCoor(startCoor);
                            Coordinate endCoor = instruction.param.newLineString.EndPoint.Coordinate;
                            CellVertex? end = FindVertexCoor(endCoor);

                            CellBoundary? boundary = null;
                            if (start == null && end == null)
                                boundary = AddBoundary(startCoor, endCoor);
                            else if (start != null && end == null)
                                boundary = AddBoundary(start, endCoor);
                            else if (start == null && end != null)
                                boundary = AddBoundary(startCoor, end);
                            else if (start != null && end != null)
                                boundary = AddBoundary(start, end);
                            if (boundary == null)
                                throw new InvalidOperationException("add boundary failed:");
                        }
                        break;
                    case Predicate.Remove:
                        {
                            CellBoundary? boundary = FindBoundaryGeom(instruction.param.oldLineString);
                            if (boundary == null)
                                throw new ArgumentException("can not find boundary: " + instruction.param.oldLineString);
                            RemoveBoundary(boundary);
                        }
                        break;
                    case Predicate.Update:
                        {
                            CellBoundary? boundary = FindBoundaryGeom(instruction.param.oldLineString);
                            if (boundary == null)
                                throw new ArgumentException("can not find boundary: " + instruction.param.oldLineString);
                            boundary.UpdateGeom(instruction.param.newLineString);
                        }
                        break;
                    default:
                        throw new ArgumentException("Unknown predicate: " + instruction.predicate);
                }
                break;
            default:
                throw new ArgumentException("Unknown subject type: " + instruction.subject);
        }
        instructionHistory.IgnoreDo = false;
    }
    public CellBoundary? AddBoundary(Coordinate startCoor, Coordinate endCoor)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { startCoor, endCoor });

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;

        var start = CellVertex.Instantiate(ls.StartPoint, IdGenVertex);
        AddVertexInternal(start);
        var end = CellVertex.Instantiate(ls.EndPoint, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary?.Gen() ?? "no id");
        instructionHistory.SessionStart();
        AddBoundaryInternal(boundary);
        instructionHistory.SessionCommit();
        ConsistencyCheck();
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { start.Coordinate, endCoor });

        if (!vertexPool.Contains(start)) throw new ArgumentException("can not find vertex start");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;

        var end = CellVertex.Instantiate(endCoor, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary?.Gen() ?? "no id");
        instructionHistory.SessionStart();
        AddBoundaryInternal(boundary);
        instructionHistory.SessionCommit();
        ConsistencyCheck();
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { startCoor, end.Coordinate });

        if (!vertexPool.Contains(end)) throw new ArgumentException("can not find vertex end");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;

        var start = CellVertex.Instantiate(startCoor, IdGenVertex);
        AddVertexInternal(start);

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary?.Gen() ?? "no id");
        instructionHistory.SessionStart();
        AddBoundaryInternal(boundary);
        instructionHistory.SessionCommit();
        ConsistencyCheck();
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
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

        if (!vertexPool.Contains(start)) throw new ArgumentException("can not find vertex start");
        if (!vertexPool.Contains(end)) throw new ArgumentException("can not find vertex end");
        if (System.Object.ReferenceEquals(start, end)) throw new ArgumentException("should not connect same vertex");

        foreach (CellBoundary b in boundaryPool)
            if (b.Geom.Crosses(ls))
                return null;
        if (VertexPair2Boundaries(start, end).Count > 0) return null;  // don't support multiple boundary between two vertices yet

        CellBoundary boundary = new CellBoundary(ls, start, end, IdGenBoundary?.Gen() ?? "no id");

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
        instructionHistory.SessionStart();
        AddBoundaryInternal(boundary);
        instructionHistory.SessionCommit();

        // can not reach
        if (ring1.Count < 2 && ring2.Count < 2) return boundary;

        var gf = new GeometryFactory();
        bool path1IsCCW = gf.CreateLinearRing(ring1.ToArray()).IsCCW;
        bool path2IsCCW = gf.CreateLinearRing(ring2.ToArray()).IsCCW;

        CellSpace cellSpace1 = CreateCellSpaceInternal(jumps1);
        CellSpace cellSpace2 = CreateCellSpaceInternal(jumps2);
        CellSpace? oldCellSpace = spacePool.FirstOrDefault(cs => cs.Polygon.Contains(MiddlePoint(ls)));

        NewCellSpaceCase ncsCase;
        if (oldCellSpace == null)
            ncsCase = NewCellSpaceCase.NewCellSpace;
        else if (path1IsCCW && path2IsCCW)
            ncsCase = NewCellSpaceCase.Split;
        else if (oldCellSpace.Polygon.Shell.Touches(start.Geom) || oldCellSpace.Polygon.Shell.Touches(end.Geom))
            ncsCase = NewCellSpaceCase.SplitNeedReSearch;
        else
            ncsCase = NewCellSpaceCase.HoleOfAnother;

        switch (ncsCase)
        {
            case NewCellSpaceCase.NewCellSpace:
                if (path1IsCCW)
                    AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps1));
                else
                    AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps2));
                Debug.Log("create new cellspace");
                break;

            case NewCellSpaceCase.Split:
                RemoveSpaceInternal(oldCellSpace!);
                AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps1));
                AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps2));
                Debug.Log("split cellspace");
                break;

            case NewCellSpaceCase.SplitNeedReSearch:
                RemoveSpaceInternal(oldCellSpace!);
                AddSpaceConsiderHole(CreateCellSpaceWithHole(reJumps1));
                AddSpaceConsiderHole(CreateCellSpaceWithHole(reJumps2));
                Debug.Log("split cellspace");
                break;

            case NewCellSpaceCase.HoleOfAnother:
                if (path1IsCCW)
                    AddSpaceConsiderHole(cellSpace1);
                else
                    AddSpaceConsiderHole(cellSpace2);
                Debug.Log("add hole to cellspace");
                break;
        }
        ConsistencyCheck();
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return boundary;
    }

    public CellVertex SplitBoundary(CellBoundary boundary, Coordinate middleCoor)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("unknown boundary");
        if (boundary.Geom.NumPoints > 2) throw new ArgumentException("We don't support split boundary with point more than 2 yet");
        Debug.Log("split boundary");
        instructionHistory.SessionStart();

        // Create vertex
        CellVertex middleVertex = CellVertex.Instantiate(middleCoor, IdGenVertex);
        AddVertexInternal(middleVertex);


        // Remove old boundary
        RemoveBoundaryInternal(boundary);


        // Create and add new boundary
        CellBoundary newBoundary1 = new CellBoundary(boundary.P0, middleVertex, IdGenBoundary?.Gen() ?? "no id");
        CellBoundary newBoundary2 = new CellBoundary(middleVertex, boundary.P1, IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(newBoundary1);
        AddBoundaryInternal(newBoundary2);


        instructionHistory.SessionCommit();


        // update space and vertex2space indices
        List<CellSpace> spaces = Boundary2Space(boundary);
        foreach (var space in spaces)
        {
            space.SplitBoundary(boundary, newBoundary1, newBoundary2, middleVertex);
            newBoundary1.PartialBound(space);
            newBoundary2.PartialBound(space);
        }
        vertex2Spaces[middleVertex] = new HashSet<CellSpace>(spaces);
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return middleVertex;
    }

    public void UpdateVertices(List<CellVertex> vertices, List<Coordinate> newCoors)
    {
        if (vertices.Count != newCoors.Count) throw new ArgumentException("vertices count should equals to coors count");

        List<Coordinate> oldCoors = vertices.Select(v => v.Coordinate).ToList();

        for (int i = 0; i < vertices.Count; i++)
            vertices[i].UpdateCoordinate(newCoors[i]);

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
                    if (s1.Polygon.Relate(s2.Geom, "T********"))  // TODO magic string
                    {
                        valid = false;
                        goto validresult;
                    }

        }
        foreach (var s in spaces)
        {
            foreach (var hole in s.Holes)
                if (!s.ShellCellSpace().Polygon.Contains(hole.Polygon.Shell))
                {
                    valid = false;
                    goto validresult;
                }
        }

    validresult:

        if (valid)
        {
            vertices.ForEach(v => v.OnUpdate?.Invoke());
            instructionHistory.DoCommit(ReducedInstruction.UpdateVertices(oldCoors, newCoors));
            FullPolygonizerCheck();
            BoundaryLeftRightCheck();
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
        instructionHistory.SessionStart();
        RemoveBoundaryInternal(boundary);
        instructionHistory.SessionCommit();

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
            Debug.Log("remove cellspace because the shell broke.");
            RemoveSpaceInternal(spaces[0]);
        }
        else if (spaces[0].ShellCellSpace().Polygon.Contains(spaces[1].ShellCellSpace().Polygon) ||
                 spaces[1].ShellCellSpace().Polygon.Contains(spaces[0].ShellCellSpace().Polygon))  // one in the hole of another
        {
            Debug.Log("merge hole into parent");
            CellSpace parent, child;
            if (spaces[0].ShellCellSpace().Polygon.Contains(spaces[1].ShellCellSpace().Polygon))
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
                UpdateSpaceInternal(parent, child, new List<CellSpace>());
                RelateVertexSpace(parent);
                RemoveSpaceInternal(child);
            }
            else
            {
                List<JumpInfo> path = PSLGPolygonSearcher.Search(new JumpInfo() { target = boundary.P0, through = boundary }, boundary.P0, AdjacentFinder);
                List<CellSpace> holes = CreateCellSpaceMulti(path);
                UpdateSpaceInternal(parent, child, holes);
                RelateVertexSpace(parent);
                RemoveSpaceInternal(child);
            }
        }
        else  // Two parallel cellspace. merge them
        {
            List<JumpInfo> path = PSLGPolygonSearcher.Search(new JumpInfo() { target = boundary.P0, through = boundary }, boundary.P0, AdjacentFinder, true, true);

            RemoveSpaceInternal(spaces[0]);
            RemoveSpaceInternal(spaces[1]);
            AddSpaceConsiderHole(CreateCellSpaceWithHole(path));
        }
        ConsistencyCheck();
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
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

    public void AddVertexInternal(CellVertex vertex)
    {
        vertexPool.Add(vertex);
        vertex2Boundaries[vertex] = new HashSet<CellBoundary>();
        vertex2Spaces[vertex] = new HashSet<CellSpace>();
        OnVertexCreated?.Invoke(vertex);

        // history.Do(ReducedInstruction.AddVertex(vertex));
    }

    private void RemoveVertexInternal(CellVertex vertex)
    {
        vertexPool.Remove(vertex);
        vertex2Boundaries.Remove(vertex);
        vertex2Spaces.Remove(vertex);
        OnVertexRemoved?.Invoke(vertex);

        // history.Do(ReducedInstruction.RemoveVertex(vertex));
    }

    private void AddBoundaryInternal(CellBoundary boundary)
    {
        if (boundaryPool.Contains(boundary)) throw new ArgumentException("add redundant cell boundary");

        boundaryPool.Add(boundary);

        vertex2Boundaries[boundary.P0].Add(boundary);
        vertex2Boundaries[boundary.P1].Add(boundary);

        OnBoundaryCreated.Invoke(boundary);

        instructionHistory.DoStep(ReducedInstruction.AddBoundary(boundary));
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

        instructionHistory.DoStep(ReducedInstruction.RemoveBoundary(boundary));
    }

    private string AddSpaceInternal(CellSpace space)
    {
        if (spacePool.Contains(space)) throw new ArgumentException("add redundant space");
        space.Id = IdGenSpace?.Gen() ?? "no id";
        spacePool.Add(space);
        RelateVertexSpace(space);
        space.allBoundaries.ForEach(b => b.PartialBound(space));
        OnSpaceCreated?.Invoke(space);
        return space.Id;
    }

    private void RemoveSpaceInternal(CellSpace space)
    {
        if (!spacePool.Contains(space)) throw new ArgumentException("Can not find the space");
        spacePool.Remove(space);
        foreach (var vertex in space.allVertices)
            vertex2Spaces[vertex].Remove(space);
        space.allBoundaries.ForEach(b => b.PartialUnBound(space));
        OnSpaceRemoved?.Invoke(space);
    }

    private void UpdateSpaceInternal(CellSpace space, CellSpace? removeHoleContainThisHole, List<CellSpace> addHoles)
    {
        List<CellBoundary> oldAllBoundaries = new List<CellBoundary>(space.allBoundaries);

        if (removeHoleContainThisHole != null)
            space.RemoveHole(removeHoleContainThisHole);
        addHoles.ForEach(hole => space.AddHole(hole));

        List<CellBoundary> newAllBoundaries = new List<CellBoundary>(space.allBoundaries);
        foreach (var b in oldAllBoundaries)
            if (!newAllBoundaries.Contains(b))
                b.PartialUnBound(space);
        foreach (var b in newAllBoundaries)
            if (!oldAllBoundaries.Contains(b))
                b.PartialBound(space);
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

    private string AddSpaceConsiderHole(CellSpace current)
    {
        CellSpace? spaceContainCurrent = null;
        List<CellSpace> holeOfCurrent = new List<CellSpace>();

        foreach (CellSpace space in spacePool)
        {
            if (space.Polygon.Contains(current.Polygon.Shell))
                if (spaceContainCurrent == null)
                    spaceContainCurrent = space;
                else
                    throw new InvalidOperationException("more than one space contain current space");
            if (current.Polygon.Contains(space.Geom))
                holeOfCurrent.Add(space);
        }

        if (spaceContainCurrent != null)
        {
            UpdateSpaceInternal(spaceContainCurrent, null, new List<CellSpace>() { current });
            RelateVertexSpace(spaceContainCurrent);
        }

        UpdateSpaceInternal(current, null, holeOfCurrent);

        return AddSpaceInternal(current);
    }

    // we should merge CreateCellSpaceMulti and CreateCellSpaceWithHole to one function
    private List<CellSpace> CreateCellSpaceMulti(List<JumpInfo> path)
    {
        List<List<JumpInfo>> rings = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedVertex);

        List<CellSpace> result = rings.Select(ring => CreateCellSpaceInternal(ring)).ToList();

        double area = 0.0f;
        CellSpace shell = result.First();
        foreach (var cellspace in result)
            if (cellspace.Polygon.Area > area)
            {
                area = cellspace.Polygon.Area;
                shell = cellspace;
            }

        bool realShell = true;
        foreach (var cellspace in result)
            if (cellspace != shell && !shell.Polygon.Contains(cellspace.Geom))
            {
                realShell = false;
                break;
            }

        if (realShell)
            result.Remove(shell);

        return result;
    }

    private CellSpace CreateCellSpaceWithHole(List<JumpInfo> path)
    {
        List<List<JumpInfo>> rings = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedVertex);

        List<CellSpace> cellSpaces = rings.Select(ring => CreateCellSpaceInternal(ring)).ToList();

        double area = 0.0f;
        CellSpace shell = cellSpaces.First();
        foreach (var cellspace in cellSpaces)
            if (cellspace.Polygon.Area > area)
            {
                area = cellspace.Polygon.Area;
                shell = cellspace;
            }
        UpdateSpaceInternal(shell, null, cellSpaces.Where(cs => cs != shell).ToList());
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

    public string CalcDigest()
        => CalcDigest(Digest.CellSpaceList(spacePool));
    public string CalcDigest(string spacesDigest)
    => "{" +
        $"vertexPool.Count: {vertexPool.Count}, " +
        $"boundaryPool digest: {Digest.CellBoundaryList(boundaryPool)}, " +
        $"spacePool digest: {spacesDigest}" +
        "}";


    private void FullPolygonizerCheck()
    {
        string expectDigest = CalcDigest(Digest.PolygonList(Polygonizer().Select(geom => (Polygon)geom).ToList()));
        string increaseDigest = CalcDigest();
        if (expectDigest != increaseDigest)
        {
            Debug.Log(expectDigest);
            Debug.Log(increaseDigest);
            throw new Exception("full Polygonizer mistmatch");
        }
    }

    private void BoundaryLeftRightCheck()
    {
        Dictionary<CellBoundary, int> sideCount = new Dictionary<CellBoundary, int>();
        boundaryPool.ForEach(b => sideCount.Add(b, 0));

        spacePool.ForEach(space => space.allBoundaries.ForEach(b
            =>
        {
            if (b.leftSpace != space && b.rightSpace != space)
                throw new Exception($"space({space.Id}) should be one of side of boundary({b.Id})");
            sideCount[b]++;
        }));
        foreach (var pair in sideCount)
        {
            if (pair.Value == 0)
            {
                if (pair.Key.leftSpace != null)
                    throw new Exception($"left space of boundary({pair.Key.Id}) should be null but it is space({pair.Key.leftSpace.Id})");
                if (pair.Key.rightSpace != null)
                    throw new Exception($"right space of boundary({pair.Key.Id}) should be null but it is space({pair.Key.rightSpace.Id})");
            }
            else if (pair.Value == 1)
            {
                if (pair.Key.leftSpace == null && pair.Key.rightSpace == null)
                    throw new Exception($"boundary({pair.Key.Id}) there should be one side space but have no one");
                if (pair.Key.leftSpace != null && pair.Key.rightSpace != null)
                    throw new Exception($"boundary({pair.Key.Id}) should have only 1 side space but have two: {pair.Key.leftSpace.Id}, {pair.Key.rightSpace.Id}");
            }
            else if (pair.Value == 2)
            {
                if (pair.Key.leftSpace == null)
                    throw new Exception($"left space of boundary({pair.Key.Id}) should not be null");
                if (pair.Key.rightSpace == null)
                    throw new Exception($"right space of boundary({pair.Key.Id}) should not be null");
            }
            else
                throw new Exception($"more than 2({pair.Value}) space contain boundary({pair.Key.Id})");
        }
    }

    [JsonIgnore] private static bool consistencyChecking = true;
    private void ConsistencyCheck()
    {
        if (consistencyChecking) return;
        Debug.Log("Polygonizer().Count, spacePoll.count " + Polygonizer().Count + " " + spacePool.Count);

        consistencyChecking = true;

        string before = CalcDigest();
        string beforeAll = Serialize();

        List<CellBoundary> boundaries = new List<CellBoundary>(boundaryPool);
        bool valid = true;
        IndoorTiling? tempIndoorTiling = null;
        foreach (var boundary in boundaries)
        {
            Debug.Log("Remove " + boundary.Id);
            tempIndoorTiling = new IndoorTiling(this);
            tempIndoorTiling.RemoveBoundary(boundary);

            Debug.Log("Add back " + boundary.Id);
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

            string after = tempIndoorTiling.CalcDigest();
            if (before != after)
            {
                Debug.LogError(boundary.Id);
                Debug.LogError(boundary.Geom);
                Debug.LogError(before);
                Debug.LogError(after);
                valid = false;
            }
            tempIndoorTiling = null;

            string afterAll = Serialize();
            if (beforeAll != afterAll)
            {
                Debug.LogError(beforeAll);
                Debug.LogError(afterAll);
                throw new Exception("Oops");
            }
        }


        if (valid)
            Debug.Log($"ConsistencyCheck OK after try remove {boundaries.Count} boundaries");

        consistencyChecking = false;
    }




}
