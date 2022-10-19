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
    public ThematicLayer layer { get; private set; }
    public string digestCache = "";
    public IDGenInterface? IdGenVertex { get; private set; }
    public IDGenInterface? IdGenBoundary { get; private set; }
    public IDGenInterface? IdGenSpace { get; private set; }

    private bool resultValidate { get; set; } = true;

#pragma warning disable CS8618
    public IndoorTiling() { }  // for deserialize only
#pragma warning restore CS8618

    // use same IndoorTiling for all ThematicLayer
    public IndoorTiling(ThematicLayer indoorData, IDGenInterface IdGenVertex, IDGenInterface IdGenBoundary, IDGenInterface IdGenSpace)
    {
        this.layer = indoorData;
        this.IdGenVertex = IdGenVertex;
        this.IdGenBoundary = IdGenBoundary;
        this.IdGenSpace = IdGenSpace;
    }

    public string Serialize(bool indent = false)
    {
        digestCache = layer.CalcDigest();
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

    public void AssignIndoorData(ThematicLayer indoorData)
    {
        this.layer = indoorData;

        IdGenVertex!.Reset();
        IdGenBoundary!.Reset();
        IdGenSpace!.Reset();
        this.layer.cellVertexMember.ForEach(v => v.Id = IdGenVertex!.Gen());
        this.layer.cellBoundaryMember.ForEach(b => b.Id = IdGenBoundary!.Gen());
        this.layer.cellSpaceMember.ForEach(s => s.Id = IdGenSpace!.Gen());
    }

    public CellBoundary? AddBoundaryAutoSnap(Coordinate startCoor, Coordinate endCoor, string? id = null)
    {
        CellVertex? startVertex = layer.FindVertexCoor(startCoor);
        CellVertex? endVertex = layer.FindVertexCoor(endCoor);
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

        if (layer.CrossesBoundaries(ls)) return null;

        var start = CellVertex.Instantiate(ls.StartPoint, IdGenVertex);
        AddVertexInternal(start);
        var end = CellVertex.Instantiate(ls.EndPoint, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(start, end, id != null ? id : IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(boundary);
        ResultValidate();
        return boundary;
    }

    public CellBoundary? AddBoundary(CellVertex start, Coordinate endCoor, string? id = null)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { start.Coordinate, endCoor });

        if (!layer.Contains(start)) throw new ArgumentException("can not find vertex start");

        if (layer.CrossesBoundaries(ls)) return null;

        var end = CellVertex.Instantiate(endCoor, IdGenVertex);
        AddVertexInternal(end);

        CellBoundary boundary = new CellBoundary(start, end, id != null ? id : IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(boundary);
        ResultValidate();
        return boundary;
    }

    public CellBoundary? AddBoundary(Coordinate startCoor, CellVertex end, string? id = null)
    {
        LineString ls = new GeometryFactory().CreateLineString(new Coordinate[] { startCoor, end.Coordinate });

        if (!layer.Contains(end)) throw new ArgumentException("can not find vertex end");

        if (layer.CrossesBoundaries(ls)) return null;

        var start = CellVertex.Instantiate(startCoor, IdGenVertex);
        AddVertexInternal(start);

        CellBoundary boundary = new CellBoundary(start, end, id != null ? id : IdGenBoundary?.Gen() ?? "no id");
        AddBoundaryInternal(boundary);
        ResultValidate();
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

        if (!layer.Contains(start)) throw new ArgumentException("can not find vertex start");
        if (!layer.Contains(end)) throw new ArgumentException("can not find vertex end");
        if (System.Object.ReferenceEquals(start, end)) throw new ArgumentException("should not connect same vertex");

        if (layer.CrossesBoundaries(ls)) return null;
        if (layer.VertexPair2Boundaries(start, end).Count > 0) return null;  // don't support multiple boundary between two vertices yet

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
        CellSpace? oldCellSpace = layer.cellSpaceMember.FirstOrDefault(cs => cs.Polygon.Contains(ThematicLayer.MiddlePoint(ls)));

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
        ResultValidate();
        return boundary;
    }

    public CellVertex SplitBoundary(Coordinate middleCoor, out CellBoundary oldBoundary, out CellBoundary newBoundary1, out CellBoundary newBoundary2)
    {
        CellBoundary? boundary = layer.FindBoundaryGeom(middleCoor, 1e-3);
        if (boundary == null) throw new ArgumentException("can not find a boundary according to coordinate: " + middleCoor.ToString());

        oldBoundary = boundary;
        return SplitBoundary(middleCoor, boundary, out newBoundary1, out newBoundary2);
    }

    public CellVertex SplitBoundary(Coordinate middleCoor, CellBoundary oldBoundary, out CellBoundary newBoundary1, out CellBoundary newBoundary2)
    {
        if (!layer.Contains(oldBoundary)) throw new ArgumentException("unknown boundary");
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
        newBoundary1.Navigable = oldBoundary.Navigable;
        newBoundary2.Navigable = oldBoundary.Navigable;
        newBoundary1.NaviDir = oldBoundary.NaviDir;
        newBoundary2.NaviDir = oldBoundary.NaviDir;
        AddBoundaryInternal(newBoundary1);
        AddBoundaryInternal(newBoundary2);

        // Construct new spaces and Add
        foreach (var space in spaces)
        {
            space.SplitBoundary(oldBoundary, newBoundary1, newBoundary2, middleVertex);
            AddSpaceInternal(space);
        }

        ResultValidate();
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
            if (layer.Contains(vertex))
            {
                boundaries.UnionWith(layer.Vertex2Boundaries(vertex));
                spaces.UnionWith(layer.Vertex2Spaces(vertex));
            }
            else throw new ArgumentException("can not find vertex");
        Debug.Log("related boundaries: " + boundaries.Count);
        Debug.Log("related spaces    : " + spaces.Count);

        foreach (var b in boundaries)
            b.UpdateFromVertex();

        bool valid = true;
        foreach (var b1 in boundaries)
        {
            foreach (var b2 in layer.cellBoundaryMember)
                if (!System.Object.ReferenceEquals(b1, b2))
                    if (b1.geom.Crosses(b2.geom))
                    {
                        valid = false;
                        goto VALID_RESULT;
                    }
        }

        foreach (var s in spaces)
        {
            bool ret = s.UpdateFromVertex();
            s.OnUpdate?.Invoke();

            if (!ret)
            {
                valid = false;
                goto VALID_RESULT;
            }

            s.rLines?.UpdateGeom();
            s.rLines?.OnUpdate?.Invoke();
        }

        foreach (var s1 in spaces)
        {
            foreach (var s2 in layer.cellSpaceMember)
                if (!System.Object.ReferenceEquals(s1, s2))
                    if (s1.Polygon.Relate(s2.Geom, kInnerInterSectionDE9IMPatter))
                    {
                        valid = false;
                        Console.WriteLine("spaces inner intersects");
                        goto VALID_RESULT;
                    }

        }
        foreach (var s in spaces)
        {
            foreach (var hole in s.Holes)
                if (!s.ShellCellSpace().Polygon.Contains(hole.Polygon.Shell))
                {
                    valid = false;
                    Console.WriteLine("space lose its hole");
                    goto VALID_RESULT;
                }
        }

    VALID_RESULT:

        if (valid)
        {
            vertices.ForEach(v => v.OnUpdate?.Invoke());
            ResultValidate();

            // update boundary again because the edge depends on the centroid of space
            foreach (var s in spaces)
                s.allBoundaries.ForEach(b => b.UpdateFromVertex());
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
        if (layer.Vertex2Boundaries(boundary.P0).Count == 0)
            RemoveVertexInternal(boundary.P0);
        if (layer.Vertex2Boundaries(boundary.P1).Count == 0)
            RemoveVertexInternal(boundary.P1);
    }

    public void RemoveBoundary(CellBoundary boundary)
    {
        // Remove space
        List<CellSpace> spaces = boundary.Spaces();

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

            Navigable navigable = spaces[0].navigable;
            if (spaces[1].navigable < navigable)
                navigable = spaces[1].navigable;

            RemoveSpaceInternal(spaces[0]);
            RemoveSpaceInternal(spaces[1]);
            RemoveBoundaryWithVertex(boundary);

            List<JumpInfo> path = PSLGPolygonSearcher.Search(new JumpInfo() { target = boundary.P0, through = boundary }, boundary.P0, AdjacentFinder, true, true);
            CellSpace newCellSpace = CreateCellSpaceWithHole(path);
            newCellSpace.navigable = navigable;
            AddSpaceConsiderHole(newCellSpace);
        }
        ResultValidate();
    }

    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        layer.UpdateBoundaryNaviDirection(boundary, direction);

        boundary.leftSpace?.rLines?.OnUpdate?.Invoke();
        boundary.rightSpace?.rLines?.OnUpdate?.Invoke();
    }

    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        layer.UpdateBoundaryNavigable(boundary, navigable);

        boundary.leftSpace?.rLines?.OnUpdate?.Invoke();
        boundary.rightSpace?.rLines?.OnUpdate?.Invoke();
    }

    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        layer.UpdateSpaceNavigable(space, navigable);

        space.rLines?.OnUpdate?.Invoke();

        layer.Space2Spaces(space).ForEach(neighbor => neighbor.rLines?.OnUpdate?.Invoke());
    }

    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        layer.UpdateRLinePassType(rLines, fr, to, passType);
    }

    private List<JumpInfo> AdjacentFinder(CellVertex cv)
    {
        if (layer.Contains(cv))
            return layer.Vertex2Boundaries(cv).Select(b => new JumpInfo() { target = b.Another(cv), through = b }).ToList();
        else
        {
            Debug.LogError(cv.Id);
            throw new Exception(cv.Id);
        }
    }

    public void AddPOI(IndoorPOI poi)
    {
        if (poi.foi.Any(foi => !layer.Contains((CellSpace)foi))) throw new ArgumentException("unknow feature of interest");
        if (poi.queue.Any(item => !layer.Contains((CellSpace)item))) throw new ArgumentException("unknow queue space");
        if (!layer.Contains((CellSpace)poi.layOnSpace)) throw new ArgumentException("unknow space lay on");
        if (!poi.CanLayOn(layer.FindSpaceGeom(poi.point.Coordinate))) throw new ArgumentException("poi can not lay on the space");

        layer.AddPOI(poi);
    }

    // TODO: check all POI after boundary updated or removed
    public bool UpdatePOI(IndoorPOI poi, Coordinate coor)
    {
        CellSpace? space = layer.FindSpaceGeom(coor);
        if (poi.CanLayOn(space))
        {
            layer.UpdatePOI(poi, coor);
            Debug.Log("Can lay on");
            return true;
        }
        else
        {
            Debug.Log("Can not lay on");
            return false;
        }
    }

    public void UpdateSpaceId(CellSpace space, string newContainerId, List<string> childrenId)
    {
        string? repeatId = null;

        if (childrenId.Contains(newContainerId))
        {
            repeatId = newContainerId;
            goto OUT;
        }

        for (int i = 0; i < childrenId.Count - 1; i++)
            for (int j = i + 1; j < childrenId.Count; j++)
                if (childrenId[i] == childrenId[j])
                {
                    repeatId = childrenId[i];
                    goto OUT;
                }

        var newIds = new List<string>(childrenId);
        newIds.Add(newContainerId);
        foreach (var newId in newIds)
            foreach (var oldspace in layer.cellSpaceMember)
                foreach (var container in oldspace.AllNodeInContainerTree())
                    if (newId == container.containerId)
                    {
                        repeatId = newId;
                        goto OUT;
                    }

                OUT:
        if (repeatId != null)
        {
            throw new ArgumentException($"new id ({repeatId}) repeated");
        }
        space.containerId = newContainerId;
        space.children.Clear();
        childrenId.ForEach(childId => space.children.Add(new Container(childId)));
    }

    public void RemovePOI(IndoorPOI poi)
    {
        layer.RemovePOI(poi);
    }

    private void AddVertexInternal(CellVertex vertex)
    {
        layer.AddVertex(vertex);
    }

    private void RemoveVertexInternal(CellVertex vertex)
    {
        layer.RemoveVertex(vertex);
    }

    private void AddBoundaryInternal(CellBoundary boundary)
    {
        layer.AddBoundary(boundary);
    }

    private void RemoveBoundaryInternal(CellBoundary boundary)
    {
        layer.RemoveBoundary(boundary);
    }


    private void AddSpaceInternal(CellSpace space)
    {
        layer.AddSpace(space, IdGenSpace?.Gen() ?? "no id");

        RLineGroup rLineGroup = new RLineGroup(space);

        layer.AddRLines(rLineGroup);

        layer.Space2Spaces(space).ForEach(neighbor => neighbor.rLines?.OnUpdate?.Invoke());
    }

    private void RemoveSpaceInternal(CellSpace space)
    {
        List<CellSpace> neighbors = layer.Space2Spaces(space);

        var rLines = space.rLines!;
        layer.RemoveRLines(rLines);

        layer.RemoveSpace(space);

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

        foreach (CellSpace space in layer.cellSpaceMember)
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

    private void ResultValidate()
    {
        if (resultValidate)
        {
            FullPolygonizerCheck();
            BoundaryLeftRightCheck();
        }
    }

    public void DisableResultValidate() => resultValidate = false;
    public void EnableResultValidateAndDoOnce()
    {
        resultValidate = true;
        ResultValidate();
    }

    private void FullPolygonizerCheck()
    {
        string expectDigest = layer.CalcDigest(Digest.PolygonList(layer.Polygonizer().Select(geom => (Polygon)geom).ToList()));
        string increaseDigest = layer.CalcDigest();
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
        layer.cellBoundaryMember.ForEach(b => sideCount.Add(b, 0));

        layer.cellSpaceMember.ForEach(space => space.allBoundaries.ForEach(b =>
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
