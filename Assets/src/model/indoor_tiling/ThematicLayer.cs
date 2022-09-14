using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Newtonsoft.Json;

using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

#nullable enable

[Serializable]
public class ThematicLayer
{
    [JsonPropertyAttribute] public bool semanticExtension;
    [JsonPropertyAttribute] public ThemeLayerValueType theme;
    [JsonPropertyAttribute] public DateTime creationDate;
    [JsonPropertyAttribute] public DateTime terminationDate;
    [JsonPropertyAttribute] public string level = "";

    [JsonPropertyAttribute] public List<CellVertex> cellVertexMember { get; private set; } = new List<CellVertex>();
    [JsonPropertyAttribute] public List<CellBoundary> cellBoundaryMember { get; private set; } = new List<CellBoundary>();
    [JsonPropertyAttribute] public List<CellSpace> cellSpaceMember { get; private set; } = new List<CellSpace>();
    [JsonPropertyAttribute] public List<RLineGroup> rLineGroupMember { get; private set; } = new List<RLineGroup>();
    [JsonPropertyAttribute] public List<IndoorPOI> poiMember { get; private set; } = new List<IndoorPOI>();

    [JsonIgnore] public Action<CellVertex>? OnVertexCreated;
    [JsonIgnore] public Action<CellVertex>? OnVertexRemoved;
    [JsonIgnore] public Action<CellBoundary>? OnBoundaryCreated;
    [JsonIgnore] public Action<CellBoundary>? OnBoundaryRemoved;
    [JsonIgnore] public Action<CellSpace>? OnSpaceCreated;
    [JsonIgnore] public Action<CellSpace>? OnSpaceRemoved;
    [JsonIgnore] public Action<RLineGroup>? OnRLinesCreated;
    [JsonIgnore] public Action<RLineGroup>? OnRLinesRemoved;
    [JsonIgnore] public Action<IndoorPOI>? OnPOICreated;
    [JsonIgnore] public Action<IndoorPOI>? OnPOIRemoved;

    [JsonIgnore] public const double kFindGeomEpsilon = 1e-4;

    [JsonIgnore] private Dictionary<CellVertex, HashSet<CellBoundary>> vertex2Boundaries = new Dictionary<CellVertex, HashSet<CellBoundary>>();
    [JsonIgnore] private Dictionary<CellBoundary, HashSet<RepresentativeLine>> boundary2RLines = new Dictionary<CellBoundary, HashSet<RepresentativeLine>>();
    [JsonIgnore] private Dictionary<Container, HashSet<IndoorPOI>> space2POIs = new Dictionary<Container, HashSet<IndoorPOI>>();

    public ThematicLayer(string level)
    {
        this.level = level;
    }

    public bool Contains(CellVertex vertex) => cellVertexMember.Contains(vertex);
    public bool Contains(CellBoundary boundary) => cellBoundaryMember.Contains(boundary);
    public bool Contains(CellSpace space) => cellSpaceMember.Contains(space);
    public bool Contains(RLineGroup rLines) => rLineGroupMember.Contains(rLines);
    public bool Contains(IndoorPOI poi) => poiMember.Contains(poi);

    public bool CrossesBoundaries(LineString ls) => cellBoundaryMember.Any(b => b.geom.Crosses(ls));

    public ICollection<CellBoundary> Vertex2Boundaries(CellVertex vertex) => vertex2Boundaries[vertex];
    public ICollection<CellSpace> Vertex2Spaces(CellVertex vertex)
        => vertex2Boundaries[vertex].Select(b => b.Spaces()).SelectMany(s => s).Distinct().ToList();
    public ICollection<RepresentativeLine> Boundary2RLines(CellBoundary boundary) => boundary2RLines[boundary];
    public List<CellSpace> Space2Spaces(CellSpace space)
        => space.allBoundaries.Select(b => b.Another(space)).Where(s => s != null).Select(s => s!).ToList();

    public HashSet<IndoorPOI> Space2POIs(Container space) => space2POIs[space];

    public void AddVertex(CellVertex vertex)
    {
        if (cellVertexMember.Contains(vertex)) throw new ArgumentException("add redundant cell vertex");
        cellVertexMember.Add(vertex);
        vertex2Boundaries[vertex] = new HashSet<CellBoundary>();

        OnVertexCreated?.Invoke(vertex);
    }

    public void RemoveVertex(CellVertex vertex)
    {
        if (!cellVertexMember.Contains(vertex)) throw new ArgumentException("can not find cell vertex");
        if (vertex2Boundaries[vertex].Count == 0)
            vertex2Boundaries.Remove(vertex);
        else
            throw new InvalidOperationException("You should remove all boundary connect to this vertex first");
        cellVertexMember.Remove(vertex);

        OnVertexRemoved?.Invoke(vertex);
    }

    public void AddBoundary(CellBoundary boundary)
    {
        if (cellBoundaryMember.Contains(boundary)) throw new ArgumentException("add redundant cell boundary");
        cellBoundaryMember.Add(boundary);
        vertex2Boundaries[boundary.P0].Add(boundary);
        vertex2Boundaries[boundary.P1].Add(boundary);
        boundary2RLines[boundary] = new HashSet<RepresentativeLine>();

        OnBoundaryCreated?.Invoke(boundary);
    }

    public void RemoveBoundary(CellBoundary boundary)
    {
        if (!cellBoundaryMember.Contains(boundary)) throw new ArgumentException("can not find cell boundary");

        if (boundary2RLines[boundary].Count == 0)
            boundary2RLines.Remove(boundary);
        else
            throw new InvalidOperationException("You should remove all RLines connect to this boundary first");

        cellBoundaryMember.Remove(boundary);
        vertex2Boundaries[boundary.P0].Remove(boundary);
        vertex2Boundaries[boundary.P1].Remove(boundary);

        OnBoundaryRemoved?.Invoke(boundary);
    }

    public void AddSpace(CellSpace space, string id)
    {
        if (cellSpaceMember.Contains(space)) throw new ArgumentException("add redundant cell space");

        space.Id = id;
        cellSpaceMember.Add(space);
        space.allBoundaries.ForEach(b => b.PartialBound(space));
        space2POIs[space] = new HashSet<IndoorPOI>();

        OnSpaceCreated?.Invoke(space);
    }

    public void RemoveSpace(CellSpace space)
    {
        if (!cellSpaceMember.Contains(space)) throw new ArgumentException("Can not find the space");

        if (space.rLines != null)
            throw new InvalidOperationException("You should remove rLine first");

        if (space2POIs[space].Count == 0)
            space2POIs.Remove(space);
        else
            throw new InvalidOperationException("You should remove all pois connect to this space first");

        cellSpaceMember.Remove(space);
        space.allBoundaries.ForEach(b => b.PartialUnBound(space));

        OnSpaceRemoved?.Invoke(space);
    }

    public void AddRLines(RLineGroup rLineGroup)
    {
        if (rLineGroupMember.Contains(rLineGroup)) throw new ArgumentException("add redundant rLine group");
        rLineGroupMember.Add(rLineGroup);
        rLineGroup.space.rLines = rLineGroup;
        rLineGroup.rLines.ForEach(rl => { boundary2RLines[rl.fr].Add(rl); boundary2RLines[rl.to].Add(rl); });

        OnRLinesCreated?.Invoke(rLineGroup);
    }

    public void RemoveRLines(RLineGroup rLineGroup)
    {
        if (!rLineGroupMember.Contains(rLineGroup)) throw new ArgumentException("Can not find the rLineGroup");

        rLineGroupMember.Remove(rLineGroup);
        rLineGroup.space.rLines = null;

        rLineGroup.rLines.ForEach(rl => { boundary2RLines[rl.fr].Remove(rl); boundary2RLines[rl.to].Remove(rl); });

        OnRLinesRemoved?.Invoke(rLineGroup);
    }

    public void AddPOI(IndoorPOI poi)
    {
        if (poiMember.Contains(poi)) throw new ArgumentException("add redundant poi");

        poiMember.Add(poi);
        poi.foi.ForEach(space => space2POIs[space].Add(poi));

        OnPOICreated?.Invoke(poi);
    }

    public void UpdatePOI(IndoorPOI poi, Coordinate coor)
    {
        if (!poiMember.Contains(poi)) throw new ArgumentException("unknow poi: " + poi.id);
        poi.point = new Point(coor);
    }

    public void RemovePOI(IndoorPOI poi)
    {
        if (!poiMember.Contains(poi)) throw new ArgumentException("unknow poi: " + poi.id);
        poi.foi.ForEach(space => space2POIs[space].Remove(poi));
        poiMember.Remove(poi);

        OnPOIRemoved?.Invoke(poi);
    }

    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        if (!cellBoundaryMember.Contains(boundary)) throw new ArgumentException("unknown boundary: " + boundary.Id);
        boundary.NaviDir = direction;
    }

    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        if (!cellBoundaryMember.Contains(boundary)) throw new ArgumentException("unknown boundary: " + boundary.Id);
        boundary.Navigable = navigable;
    }

    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        if (!cellSpaceMember.Contains(space)) throw new ArgumentException("unknown space: " + space.Id);
        space.Navigable = navigable;
    }

    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        if (!rLineGroupMember.Contains(rLines)) throw new ArgumentException("unknown rLineGroup");
        rLines.SetPassType(fr, to, passType);
    }

    public CellVertex? FindVertexCoor(Point point)
        => cellVertexMember.FirstOrDefault(vertex => vertex.Geom.Distance(point) < kFindGeomEpsilon);

    public CellVertex? FindVertexCoor(Coordinate coor)
        => cellVertexMember.FirstOrDefault(vertex => vertex.Geom.Coordinate.Distance(coor) < kFindGeomEpsilon);
    public ICollection<CellBoundary> VertexPair2Boundaries(CellVertex cv1, CellVertex cv2)
        => vertex2Boundaries[cv1].Where(b => System.Object.ReferenceEquals(b.Another(cv1), cv2)).ToList();
    public CellBoundary? FindBoundaryGeom(LineString ls)
    {
        CellVertex? start = FindVertexCoor(ls.StartPoint);
        if (start == null)
            throw new ArgumentException("can not find vertex as start point of line string: " + ls.StartPoint.Coordinate);
        CellVertex? end = FindVertexCoor(ls.EndPoint);
        if (end == null)
            throw new ArgumentException("can not find vertex as end point of line string: " + ls.EndPoint.Coordinate);
        var boundaries = VertexPair2Boundaries(start, end);
        return boundaries.FirstOrDefault(b => b.geom.Distance(MiddlePoint(ls)) < kFindGeomEpsilon);
    }

    public CellSpace? FindSpaceGeom(Coordinate coor)
        => cellSpaceMember.FirstOrDefault(space => space.Polygon.Contains(new Point(coor)));

    public CellSpace? FindContainerId(string id)
        => cellSpaceMember.FirstOrDefault(space => space.Contains(id));

    public CellSpace? FindSpaceId(string id)
        => cellSpaceMember.FirstOrDefault(space => space.Id == id);

    public IndoorPOI? FindIndoorPOI(Coordinate coor)
    {
        IndoorPOI? closest = null;
        double minDistance = double.MaxValue;
        foreach (IndoorPOI poi in poiMember)
        {
            if (minDistance > poi.point.Coordinate.Distance(coor))
            {
                minDistance = poi.point.Coordinate.Distance(coor);
                closest = poi;
            }
        }
        if (minDistance > 1e-2)
            throw new Exception("closest poi too far: " + minDistance);
        return closest;
    }

    public RepresentativeLine? FindRLine(LineString ls, out RLineGroup? rLineGroup)
    {
        foreach (var rLines in rLineGroupMember)
            foreach (var rl in rLines.rLines)
                if (ls.EqualsNormalized(rl.geom))
                {
                    rLineGroup = rLines;
                    return rl;
                }
        rLineGroup = null;
        return null;
    }

    public static Point MiddlePoint(LineString ls)
    {
        if (ls.NumPoints < 2)
            throw new ArgumentException("Empty LingString don't have middlePoint");
        else if (ls.NumPoints == 2)
            return new GeometryFactory().CreatePoint(new Coordinate((ls.StartPoint.X + ls.EndPoint.X) / 2.0f, (ls.StartPoint.Y + ls.EndPoint.Y) / 2.0f));
        else
            return ls.GetPointN(1);
    }

    public void UpdateIndices()
    {
        vertex2Boundaries.Clear();
        cellVertexMember.ForEach(v => vertex2Boundaries[v] = new HashSet<CellBoundary>());
        cellBoundaryMember.ForEach(b => { vertex2Boundaries[b.P0].Add(b); vertex2Boundaries[b.P1].Add(b); });

        cellBoundaryMember.ForEach(b => b.PartialUnBoundAll());
        cellSpaceMember.ForEach(s => s.allBoundaries.ForEach(b => b.PartialBound(s)));

        cellSpaceMember.ForEach(s => s.rLines = null);
        rLineGroupMember.ForEach(rls => rls.space.rLines = rls);
        rLineGroupMember.ForEach(rls => rls.rLines.ForEach(rl => rl.UpdateGeom(rls.space)));

        boundary2RLines.Clear();
        cellBoundaryMember.ForEach(b => boundary2RLines[b] = new HashSet<RepresentativeLine>());
        rLineGroupMember.ForEach(rls => rls.rLines.ForEach(rl => { boundary2RLines[rl.fr].Add(rl); boundary2RLines[rl.to].Add(rl); }));

        space2POIs.Clear();
        cellSpaceMember.ForEach(s => space2POIs[s] = new HashSet<IndoorPOI>());
        poiMember.ForEach(poi => poi.foi.ForEach(space => space2POIs[space].Add(poi)));
    }

    public string CalcDigest()
            => CalcDigest(Digest.CellSpaceList(cellSpaceMember));

    public string CalcDigest(string spacesDigest)
    => "{" +
        $"cellVertexMember.Count: {cellVertexMember.Count}, " +
        $"cellBoundaryMember digest: {Digest.CellBoundaryList(cellBoundaryMember)}, " +
        $"cellSpaceMember digest: {spacesDigest}" +
        "}";

    public string Serialize(bool indent = true)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
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
            jsonSerializer.Serialize(jsonWriter, this);
        }

        return sw.ToString();
        // return JsonConvert.SerializeObject(this);
    }

    public static ThematicLayer? Deserialize(string json)
    {
        ThematicLayer? indoorData = JsonConvert.DeserializeObject<ThematicLayer>(json, new WKTConverter(), new CoorConverter());
        if (indoorData != null)
            indoorData.UpdateIndices();
        return indoorData;
    }

    public ICollection<Geometry> Polygonizer()
    {
        var polygonizer = new Polygonizer();
        polygonizer.Add(cellBoundaryMember.Select(b => (Geometry)b.geom).ToList());
        return polygonizer.GetPolygons();
    }
}
