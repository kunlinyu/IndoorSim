using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;

using NetTopologySuite.Geometries;
using Newtonsoft.Json;

using UnityEngine;
using UnityEditor;

using JumpInfo = PSLGPolygonSearcher.JumpInfo;

#nullable enable


[Serializable]
public class IndoorTiling
{
    public string kInnerInterSectionDE9IMPatter = "T********";
    public IndoorData indoorData { get; private set; }
    public string digestCache = "";
    public IDGenInterface? IdGenVertex { get; private set; }
    public IDGenInterface? IdGenBoundary { get; private set; }
    public IDGenInterface? IdGenSpace { get; private set; }
    public Action<CellVertex> OnVertexCreated = (v) => { };
    public Action<CellBoundary> OnBoundaryCreated = (b) => { };
    public Action<CellSpace> OnSpaceCreated = (s) => { };
    public Action<RLineGroup> OnRLinesCreated = (rLs) => { };
    public Action<CellVertex> OnVertexRemoved = (v) => { };
    public Action<CellBoundary> OnBoundaryRemoved = (b) => { };
    public Action<CellSpace> OnSpaceRemoved = (s) => { };
    public Action<RLineGroup> OnRLinesRemoved = (rLs) => { };
    public Action<IndoorPOI> OnPOICreated = (poi) => { };
    public Action<IndoorPOI> OnPOIRemoved = (poi) => { };

#pragma warning disable CS8618
    public IndoorTiling() { }  // for deserialize only
#pragma warning restore CS8618

    public IndoorTiling(IndoorData indoorData, IDGenInterface IdGenVertex, IDGenInterface IdGenBoundary, IDGenInterface IdGenSpace)
    {
        this.indoorData = indoorData;
        this.IdGenVertex = IdGenVertex;
        this.IdGenBoundary = IdGenBoundary;
        this.IdGenSpace = IdGenSpace;
    }

    public string Serialize(bool indent = false)
    {
        digestCache = indoorData.CalcDigest();
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            Formatting = indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new WKTConverter(), new CoorConverter() },
        };

        JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
        StringBuilder sb = new StringBuilder(256);
        StringWriter sw = new StringWriter(sb);
        using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
        {
            jsonWriter.Formatting = jsonSerializer.Formatting;
            jsonWriter.IndentChar = '\t';
            jsonWriter.Indentation = 1;
            jsonSerializer.Serialize(jsonWriter, this, null);
        }

        return sw.ToString();
        // return JsonConvert.SerializeObject(this);
    }

    public void AssignIndoorData(IndoorData indoorData)
    {
        this.indoorData.vertexPool.ForEach(v => OnVertexRemoved?.Invoke(v));
        this.indoorData.boundaryPool.ForEach(b => OnBoundaryRemoved?.Invoke(b));
        this.indoorData.spacePool.ForEach(s => OnSpaceRemoved?.Invoke(s));
        this.indoorData.rLinePool.ForEach(r => OnRLinesRemoved?.Invoke(r));
        this.indoorData.pois.ForEach(p => OnPOIRemoved?.Invoke(p));

        this.indoorData = indoorData;

        this.indoorData.vertexPool.ForEach(v => OnVertexCreated?.Invoke(v));
        this.indoorData.boundaryPool.ForEach(b => OnBoundaryCreated?.Invoke(b));
        this.indoorData.spacePool.ForEach(s => OnSpaceCreated?.Invoke(s));
        this.indoorData.rLinePool.ForEach(r => OnRLinesCreated?.Invoke(r));
        this.indoorData.pois.ForEach(p => OnPOICreated?.Invoke(p));


        IdGenVertex!.Reset();
        IdGenBoundary!.Reset();
        IdGenSpace!.Reset();
        this.indoorData.vertexPool.ForEach(v => v.Id = IdGenVertex!.Gen());
        this.indoorData.boundaryPool.ForEach(b => b.Id = IdGenBoundary!.Gen());
        this.indoorData.spacePool.ForEach(s => s.Id = IdGenSpace!.Gen());
    }

    public bool DeserializeInPlace(string json, bool historyOnly = false)
    {
        foreach (var v in indoorData.vertexPool)
            OnVertexRemoved?.Invoke(v);
        foreach (var b in indoorData.boundaryPool)
            OnBoundaryRemoved?.Invoke(b);
        foreach (var s in indoorData.spacePool)
            OnSpaceRemoved?.Invoke(s);
        foreach (var r in indoorData.rLinePool)
            OnRLinesRemoved?.Invoke(r);
        indoorData = new IndoorData();


        IndoorTiling? indoorTiling = Deserialize(json, historyOnly);
        if (indoorTiling == null) return false;


        if (!historyOnly)
        {
            indoorData = indoorTiling.indoorData;
            foreach (var v in indoorData.vertexPool)
                OnVertexCreated?.Invoke(v);
            foreach (var b in indoorData.boundaryPool)
                OnBoundaryCreated?.Invoke(b);
            foreach (var s in indoorData.spacePool)
                OnSpaceCreated?.Invoke(s);
            foreach (var r in indoorData.rLinePool)
                OnRLinesCreated?.Invoke(r);
        }

        IdGenVertex!.Reset();
        foreach (var v in indoorData.vertexPool)
            v.Id = IdGenVertex!.Gen();
        IdGenBoundary!.Reset();
        foreach (var b in indoorData.boundaryPool)
            b.Id = IdGenBoundary!.Gen();
        IdGenSpace!.Reset();
        foreach (var s in indoorData.spacePool)
            s.Id = IdGenSpace!.Gen();

        return true;
    }

    public static IndoorTiling? Deserialize(string json, bool historyOnly = false)
    {
        IndoorTiling? indoorTiling = JsonConvert.DeserializeObject<IndoorTiling>(json, new WKTConverter(), new CoorConverter(), new StackConverter());
        if (indoorTiling == null) return null;

        if (historyOnly)
        {
            indoorTiling.indoorData = new IndoorData();
            // indoorTiling.instructionHistory.Uuundo();
        }
        else
        {
            if (indoorTiling.indoorData != null)
                indoorTiling.indoorData.UpdateIndices();
        }
        return indoorTiling;
    }


    public CellBoundary? AddBoundaryAutoSnap(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellVertex? startVertex = indoorData.FindVertexCoor(startCoor);
        CellVertex? endVertex = indoorData.FindVertexCoor(endCoor);
        if (startVertex != null && endVertex != null)
            return AddBoundary(startVertex, endVertex, id);
        else if (startVertex != null && endVertex == null)
            return AddBoundary(startVertex, endCoor, id);
        else if (startVertex == null && endVertex != null)
            return AddBoundary(startCoor, endVertex, id);
        else
            return AddBoundary(startCoor, endCoor, id);

    }
    public CellBoundary? AddBoundary(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { startCoor, endCoor });

        if (indoorData.CrossesBoundaries(ls)) return null;

        var start = CellVertex.Instantiate(ls.StartPoint, IdGenVertex);
        AddVertexInternal(start);
        var end = CellVertex.Instantiate(ls.EndPoint, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(start, end, id != null ? id : IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(boundary);
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor, string? id = null)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { start.Coordinate, endCoor });

        if (!indoorData.Contains(start)) throw new ArgumentException("can not find vertex start");

        if (indoorData.CrossesBoundaries(ls)) return null;

        var end = CellVertex.Instantiate(endCoor, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(start, end, id != null ? id : IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(boundary);
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end, string? id = null)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { startCoor, end.Coordinate });

        if (!indoorData.Contains(end)) throw new ArgumentException("can not find vertex end");

        if (indoorData.CrossesBoundaries(ls)) return null;

        var start = CellVertex.Instantiate(startCoor, IdGenVertex);
        AddVertexInternal(start);

        CellBoundary boundary = new CellBoundary(start, end, id != null ? id : IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(boundary);
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
    public CellBoundary? AddBoundary(CellVertex start, CellVertex end, string? id = null)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { start.Coordinate, end.Coordinate });

        if (!indoorData.Contains(start)) throw new ArgumentException("can not find vertex start");
        if (!indoorData.Contains(end)) throw new ArgumentException("can not find vertex end");
        if (System.Object.ReferenceEquals(start, end)) throw new ArgumentException("should not connect same vertex");

        if (indoorData.CrossesBoundaries(ls)) return null;
        if (indoorData.VertexPair2Boundaries(start, end).Count > 0) return null;  // don't support multiple boundary between two vertices yet

        CellBoundary boundary = new CellBoundary(start, end, id != null ? id : IdGenBoundary?.Gen() ?? "no id");

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
        CellSpace? oldCellSpace = indoorData.spacePool.FirstOrDefault(cs => cs.Polygon.Contains(IndoorData.MiddlePoint(ls)));

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
                AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps1, oldCellSpace!.Navigable));
                AddSpaceConsiderHole(CreateCellSpaceWithHole(jumps2, oldCellSpace!.Navigable));
                Debug.Log("split cellspace");
                break;

            case NewCellSpaceCase.SplitNeedReSearch:
                RemoveSpaceInternal(oldCellSpace!);
                AddSpaceConsiderHole(CreateCellSpaceWithHole(reJumps1, oldCellSpace!.Navigable));
                AddSpaceConsiderHole(CreateCellSpaceWithHole(reJumps2, oldCellSpace!.Navigable));
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
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return boundary;
    }

    public CellVertex SplitBoundary(CellBoundary oldBoundary, Coordinate middleCoor, out CellBoundary newBoundary1, out CellBoundary newBoundary2)
    {
        if (!indoorData.Contains(oldBoundary)) throw new ArgumentException("unknown boundary");
        if (oldBoundary.geom.NumPoints > 2) throw new ArgumentException("We don't support split boundary with point more than 2 yet");
        // TODO(robust): check middleCoor lay on the old boundary, or we have to check new boundary won't crosses other boundaries
        Debug.Log("split boundary");

        // Remove spaces
        List<CellSpace> spaces = oldBoundary.Spaces().ToList();
        spaces.ForEach(s => RemoveSpaceInternal(s));

        // Remove old boundary
        RemoveBoundaryInternal(oldBoundary);

        // Add new vertex
        CellVertex middleVertex = CellVertex.Instantiate(middleCoor, IdGenVertex);
        AddVertexInternal(middleVertex);

        // Create and add new boundaries
        newBoundary1 = new CellBoundary(oldBoundary.P0, middleVertex, IdGenBoundary?.Gen() ?? "no id");
        newBoundary2 = new CellBoundary(middleVertex, oldBoundary.P1, IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(newBoundary1);
        AddBoundaryInternal(newBoundary2);

        // Construct new spaces and Add
        foreach (var space in spaces)
        {
            space.SplitBoundary(oldBoundary, newBoundary1, newBoundary2, middleVertex);
            AddSpaceInternal(space);
        }

        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
        return middleVertex;
    }

    public bool UpdateVertices(List<CellVertex> vertices, List<Coordinate> newCoors)
    {
        if (vertices.Count != newCoors.Count) throw new ArgumentException("vertices count should equals to coors count");

        List<Coordinate> oldCoors = vertices.Select(v => v.Coordinate).ToList();

        for (int i = 0; i < vertices.Count; i++)
            vertices[i].UpdateCoordinate(newCoors[i]);

        HashSet<CellBoundary> boundaries = new HashSet<CellBoundary>();
        HashSet<CellSpace> spaces = new HashSet<CellSpace>();
        foreach (var vertex in vertices)
            if (indoorData.Contains(vertex))
            {
                boundaries.UnionWith(indoorData.Vertex2Boundaries(vertex));
                spaces.UnionWith(indoorData.Vertex2Spaces(vertex));
            }
            else throw new ArgumentException("can not find vertex");
        Debug.Log("related boundaries: " + boundaries.Count);
        Debug.Log("related spaces    : " + spaces.Count);

        foreach (var b in boundaries)
            b.UpdateFromVertex();

        bool valid = true;
        foreach (var b1 in boundaries)
        {
            foreach (var b2 in indoorData.boundaryPool)
                if (!System.Object.ReferenceEquals(b1, b2))
                    if (b1.geom.Crosses(b2.geom))
                    {
                        valid = false;
                        goto VALID_RESULT;
                    }
        }

        foreach (var s in spaces)
        {
            s.UpdateFromVertex();
            s.OnUpdate?.Invoke();

            s.rLines?.UpdateGeom();
            s.rLines?.OnUpdate?.Invoke();
        }

        foreach (var s1 in spaces)
        {
            foreach (var s2 in indoorData.spacePool)
                if (!System.Object.ReferenceEquals(s1, s2))
                    if (s1.Polygon.Relate(s2.Geom, kInnerInterSectionDE9IMPatter))
                    {
                        valid = false;
                        goto VALID_RESULT;
                    }

        }
        foreach (var s in spaces)
        {
            foreach (var hole in s.Holes)
                if (!s.ShellCellSpace().Polygon.Contains(hole.Polygon.Shell))
                {
                    valid = false;
                    goto VALID_RESULT;
                }
        }

    VALID_RESULT:

        if (valid)
        {
            vertices.ForEach(v => v.OnUpdate?.Invoke());
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

        return valid;
    }

    private void RemoveBoundaryWithVertex(CellBoundary boundary)
    {
        RemoveBoundaryInternal(boundary);

        // Remove Vertex if no boundary connect to it
        if (indoorData.Vertex2Boundaries(boundary.P0).Count == 0)
            RemoveVertexInternal(boundary.P0);
        if (indoorData.Vertex2Boundaries(boundary.P1).Count == 0)
            RemoveVertexInternal(boundary.P1);
    }

    public void RemoveBoundary(CellBoundary boundary)
    {
        // Remove space
        List<CellSpace> spaces = new List<CellSpace>(boundary.Spaces());
        if (spaces.Count == 0)  // no cellspace related
        {
            Debug.Log("remove boundary without any space");
            RemoveBoundaryWithVertex(boundary);
        }
        else if (spaces.Count == 1)  // only 1 cellspace related. Remove the cellspace.
        {
            Debug.Log("remove cellspace because the shell broke.");
            RemoveSpaceInternal(spaces[0]);
            RemoveBoundaryWithVertex(boundary);
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
                RemoveSpaceInternal(child);
                RemoveSpaceInternal(parent);
                RemoveBoundaryWithVertex(boundary);
                UpdateHoleOfSpace(parent, child, new List<CellSpace>());
                AddSpaceInternal(parent);
            }
            else
            {
                RemoveSpaceInternal(child);
                RemoveSpaceInternal(parent);
                RemoveBoundaryWithVertex(boundary);

                List<JumpInfo> path = PSLGPolygonSearcher.Search(new JumpInfo() { target = boundary.P0, through = boundary }, boundary.P0, AdjacentFinder);
                List<CellSpace> holes = CreateCellSpaceMulti(path);
                UpdateHoleOfSpace(parent, child, holes);
                AddSpaceInternal(parent);
            }
        }
        else  // Two parallel cellspace. merge them
        {
            Debug.Log("merge two spaces");

            RemoveSpaceInternal(spaces[0]);
            RemoveSpaceInternal(spaces[1]);
            RemoveBoundaryWithVertex(boundary);

            List<JumpInfo> path = PSLGPolygonSearcher.Search(new JumpInfo() { target = boundary.P0, through = boundary }, boundary.P0, AdjacentFinder, true, true);
            AddSpaceConsiderHole(CreateCellSpaceWithHole(path));
        }
        FullPolygonizerCheck();
        BoundaryLeftRightCheck();
    }

    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        indoorData.UpdateBoundaryNaviDirection(boundary, direction);

        boundary.leftSpace?.rLines?.OnUpdate?.Invoke();
        boundary.rightSpace?.rLines?.OnUpdate?.Invoke();
    }

    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        indoorData.UpdateBoundaryNavigable(boundary, navigable);

        boundary.leftSpace?.rLines?.OnUpdate?.Invoke();
        boundary.rightSpace?.rLines?.OnUpdate?.Invoke();
    }

    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        indoorData.UpdateSpaceNavigable(space, navigable);

        space.rLines?.OnUpdate?.Invoke();

        indoorData.Space2Spaces(space).ForEach(neighbor => neighbor.rLines?.OnUpdate?.Invoke());
    }

    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        indoorData.UpdateRLinePassType(rLines, fr, to, passType);
    }

    private List<JumpInfo> AdjacentFinder(CellVertex cv)
    {
        if (indoorData.Contains(cv))
            return indoorData.Vertex2Boundaries(cv).Select(b => new JumpInfo() { target = b.Another(cv), through = b }).ToList();
        else
        {
            Debug.LogError(cv.Id);
            throw new Exception(cv.Id);
        }
    }

    public void AddPOI(IndoorPOI poi)
    {
        indoorData.AddPOI(poi);
        OnPOICreated?.Invoke(poi);
    }

    public void UpdatePOI(IndoorPOI poi, Coordinate coor)
    {
        indoorData.UpdatePOI(poi, coor);
        // TODO: check poi lay on correct space
    }

    public void RemovePOI(IndoorPOI poi)
    {
        indoorData.RemovePOI(poi);
        OnPOIRemoved?.Invoke(poi);
    }

    private void AddVertexInternal(CellVertex vertex)
    {
        indoorData.AddVertex(vertex);
        OnVertexCreated?.Invoke(vertex);
    }

    private void RemoveVertexInternal(CellVertex vertex)
    {
        indoorData.RemoveVertex(vertex);
        OnVertexRemoved?.Invoke(vertex);
    }

    private void AddBoundaryInternal(CellBoundary boundary)
    {
        indoorData.AddBoundary(boundary);
        OnBoundaryCreated.Invoke(boundary);
    }

    private void RemoveBoundaryInternal(CellBoundary boundary)
    {
        indoorData.RemoveBoundary(boundary);
        OnBoundaryRemoved?.Invoke(boundary);
    }


    private void AddSpaceInternal(CellSpace space)
    {
        indoorData.AddSpace(space, IdGenSpace?.Gen() ?? "no id");
        OnSpaceCreated?.Invoke(space);

        RLineGroup rLineGroup = new RLineGroup(space);

        indoorData.AddRLines(rLineGroup);
        OnRLinesCreated?.Invoke(rLineGroup);

        indoorData.Space2Spaces(space).ForEach(neighbor => neighbor.rLines?.OnUpdate?.Invoke());
    }

    private void RemoveSpaceInternal(CellSpace space)
    {
        List<CellSpace> neighbors = indoorData.Space2Spaces(space);

        var rLines = space.rLines!;
        indoorData.RemoveRLines(rLines);
        OnRLinesRemoved?.Invoke(rLines);

        indoorData.RemoveSpace(space);
        OnSpaceRemoved?.Invoke(space);

        neighbors.ForEach(neighbor => neighbor.rLines?.OnUpdate?.Invoke());
    }

    private void UpdateHoleOfSpace(CellSpace space, CellSpace? removeHoleContainThisHole, List<CellSpace> addHoles)
    {
        if (removeHoleContainThisHole != null)
            space.RemoveHole(removeHoleContainThisHole);

        addHoles.ForEach(hole => space.AddHole(hole));
    }

    private void AddSpaceConsiderHole(CellSpace current)
    {
        CellSpace? spaceContainCurrent = null;
        List<CellSpace> holeOfCurrent = new List<CellSpace>();

        foreach (CellSpace space in indoorData.spacePool)
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
            RemoveSpaceInternal(spaceContainCurrent);
            UpdateHoleOfSpace(spaceContainCurrent, null, new List<CellSpace>() { current });
            AddSpaceInternal(spaceContainCurrent);
        }

        UpdateHoleOfSpace(current, null, holeOfCurrent);

        AddSpaceInternal(current);
    }

    // TODO(coding optimization): we should merge CreateCellSpaceMulti and CreateCellSpaceWithHole to one function
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

    // TODO(coding optimization): we should merge CreateCellSpaceMulti and CreateCellSpaceWithHole to one function
    private CellSpace CreateCellSpaceWithHole(List<JumpInfo> path, Navigable navigable = Navigable.Navigable)
    {
        List<List<JumpInfo>> rings = PSLGPolygonSearcher.Jumps2Rings(path, SplitRingType.SplitByRepeatedVertex);

        List<CellSpace> cellSpaces = rings.Select(ring => CreateCellSpaceInternal(ring)).ToList();

        double area = 0.0f;
        CellSpace shell = cellSpaces.First();
        shell.Navigable = navigable;
        foreach (var cellspace in cellSpaces)
            if (cellspace.Polygon.Area > area)
            {
                area = cellspace.Polygon.Area;
                shell = cellspace;
            }
        UpdateHoleOfSpace(shell, null, cellSpaces.Where(cs => cs != shell).ToList());
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


    private void FullPolygonizerCheck()
    {
        string expectDigest = indoorData.CalcDigest(Digest.PolygonList(indoorData.Polygonizer().Select(geom => (Polygon)geom).ToList()));
        string increaseDigest = indoorData.CalcDigest();
        if (expectDigest != increaseDigest)
        {
            Debug.Log(expectDigest);
            Debug.Log(increaseDigest);
            throw new Exception("full Polygonizer mismatch");
        }
    }

    private void BoundaryLeftRightCheck()
    {
        Dictionary<CellBoundary, int> sideCount = new Dictionary<CellBoundary, int>();
        indoorData.boundaryPool.ForEach(b => sideCount.Add(b, 0));

        indoorData.spacePool.ForEach(space => space.allBoundaries.ForEach(b =>
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

}
