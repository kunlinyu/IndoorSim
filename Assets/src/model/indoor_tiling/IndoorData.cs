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
public class IndoorData
{
    [JsonPropertyAttribute] public List<CellVertex> vertexPool { get; private set; } = new List<CellVertex>();
    [JsonPropertyAttribute] public List<CellBoundary> boundaryPool { get; private set; } = new List<CellBoundary>();
    [JsonPropertyAttribute] public List<CellSpace> spacePool { get; private set; } = new List<CellSpace>();
    [JsonPropertyAttribute] public List<RLineGroup> rLinePool { get; private set; } = new List<RLineGroup>();

    [JsonIgnore] public const double kFindGeomEpsilon = 1e-4;

    [JsonIgnore] private Dictionary<CellVertex, HashSet<CellBoundary>> vertex2Boundaries = new Dictionary<CellVertex, HashSet<CellBoundary>>();
    [JsonIgnore] private Dictionary<CellBoundary, HashSet<RepresentativeLine>> boundary2RLines = new Dictionary<CellBoundary, HashSet<RepresentativeLine>>();

    public bool Contains(CellVertex vertex) => vertexPool.Contains(vertex);
    public bool Contains(CellBoundary boundary) => boundaryPool.Contains(boundary);
    public bool Contains(CellSpace space) => spacePool.Contains(space);
    public bool Contains(RLineGroup rLines) => rLinePool.Contains(rLines);

    public bool CrossesBoundaries(LineString ls) => boundaryPool.Any(b => b.geom.Crosses(ls));

    public ICollection<CellBoundary> Vertex2Boundaries(CellVertex vertex) => vertex2Boundaries[vertex];
    public ICollection<CellSpace> Vertex2Spaces(CellVertex vertex)
        => vertex2Boundaries[vertex].Select(b => b.Spaces()).SelectMany(s => s).Distinct().ToList();
    public ICollection<RepresentativeLine> Boundary2RLines(CellBoundary boundary) => boundary2RLines[boundary];
    public List<CellSpace> Space2Spaces(CellSpace space)
        => space.allBoundaries.Select(b => b.Another(space)).Where(s => s != null).Select(s => s!).ToList();

    public void AddVertex(CellVertex vertex)
    {
        if (vertexPool.Contains(vertex)) throw new ArgumentException("add redundant cell vertex");
        vertexPool.Add(vertex);
        vertex2Boundaries[vertex] = new HashSet<CellBoundary>();
    }

    public void RemoveVertex(CellVertex vertex)
    {
        if (!vertexPool.Contains(vertex)) throw new ArgumentException("can not find cell vertex");
        if (vertex2Boundaries[vertex].Count == 0)
            vertex2Boundaries.Remove(vertex);
        else
            throw new InvalidOperationException("You should remove all boundary connect to this vertex first");
        vertexPool.Remove(vertex);
    }

    public void AddBoundary(CellBoundary boundary)
    {
        if (boundaryPool.Contains(boundary)) throw new ArgumentException("add redundant cell boundary");
        boundaryPool.Add(boundary);
        vertex2Boundaries[boundary.P0].Add(boundary);
        vertex2Boundaries[boundary.P1].Add(boundary);
        boundary2RLines[boundary] = new HashSet<RepresentativeLine>();
    }

    public void RemoveBoundary(CellBoundary boundary)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("can not find cell boundary");

        if (boundary2RLines[boundary].Count == 0)
            boundary2RLines.Remove(boundary);
        else
            throw new InvalidOperationException("You should remove all RLines connect to this boundary first");

        boundaryPool.Remove(boundary);
        vertex2Boundaries[boundary.P0].Remove(boundary);
        vertex2Boundaries[boundary.P1].Remove(boundary);
    }

    public void AddSpace(CellSpace space, string id)
    {
        if (spacePool.Contains(space)) throw new ArgumentException("add redundant cell space");

        space.Id = id;
        spacePool.Add(space);
        space.allBoundaries.ForEach(b => b.PartialBound(space));
    }

    public void RemoveSpace(CellSpace space)
    {
        if (!spacePool.Contains(space)) throw new ArgumentException("Can not find the space");

        if (space.rLines != null)
            throw new InvalidOperationException("You should remove rLine first");

        spacePool.Remove(space);
        space.allBoundaries.ForEach(b => b.PartialUnBound(space));
    }

    public void AddRLines(RLineGroup rLineGroup)
    {
        if (rLinePool.Contains(rLineGroup)) throw new ArgumentException("add redundant rLine group");
        rLinePool.Add(rLineGroup);
        rLineGroup.space.rLines = rLineGroup;
        rLineGroup.rLines.ForEach(rl => { boundary2RLines[rl.fr].Add(rl); boundary2RLines[rl.to].Add(rl); });
    }

    public void RemoveRLines(RLineGroup rLineGroup)
    {
        if (!rLinePool.Contains(rLineGroup)) throw new ArgumentException("Can not find the rLineGroup");

        rLinePool.Remove(rLineGroup);
        rLineGroup.space.rLines = null;

        rLineGroup.rLines.ForEach(rl => { boundary2RLines[rl.fr].Remove(rl); boundary2RLines[rl.to].Remove(rl); });
    }

    public void UpdateBoundaryNaviDirection(CellBoundary boundary, NaviDirection direction)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("unknown boundary: " + boundary.Id);
        boundary.NaviDir = direction;
    }

    public void UpdateBoundaryNavigable(CellBoundary boundary, Navigable navigable)
    {
        if (!boundaryPool.Contains(boundary)) throw new ArgumentException("unknown boundary: " + boundary.Id);
        boundary.Navigable = navigable;
    }

    public void UpdateSpaceNavigable(CellSpace space, Navigable navigable)
    {
        if (!spacePool.Contains(space)) throw new ArgumentException("unknown space: " + space.Id);
        space.Navigable = navigable;
    }

    public void UpdateRLinePassType(RLineGroup rLines, CellBoundary fr, CellBoundary to, PassType passType)
    {
        if (!rLinePool.Contains(rLines)) throw new ArgumentException("unknown rLineGroup");
        rLines.SetPassType(fr, to, passType);
    }

    public CellVertex? FindVertexCoor(Point coor)
    => vertexPool.FirstOrDefault(vertex => vertex.Geom.Distance(coor) < kFindGeomEpsilon);

    public CellVertex? FindVertexCoor(Coordinate coor)
        => vertexPool.FirstOrDefault(vertex => vertex.Geom.Coordinate.Distance(coor) < kFindGeomEpsilon);
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
        => spacePool.FirstOrDefault(space => space.Polygon.Contains(new Point(coor)));

    public CellSpace? FindSpaceId(string id)
        => spacePool.FirstOrDefault(space => space.Contains(id));

    public RepresentativeLine? FindRLine(LineString ls, out RLineGroup? rLineGroup)
    {
        foreach (var rLines in rLinePool)
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
        vertexPool.ForEach(v => vertex2Boundaries[v] = new HashSet<CellBoundary>());
        boundaryPool.ForEach(b => { vertex2Boundaries[b.P0].Add(b); vertex2Boundaries[b.P1].Add(b); });

        boundaryPool.ForEach(b => b.PartialUnBoundAll());
        spacePool.ForEach(s => s.allBoundaries.ForEach(b => b.PartialBound(s)));

        spacePool.ForEach(s => s.rLines = null);
        rLinePool.ForEach(rl => rl.space.rLines = rl);

        boundary2RLines.Clear();
        boundaryPool.ForEach(b => boundary2RLines[b] = new HashSet<RepresentativeLine>());
        rLinePool.ForEach(rls => rls.rLines.ForEach(rl => { boundary2RLines[rl.fr].Add(rl); boundary2RLines[rl.to].Add(rl); }));
    }

    public string CalcDigest()
            => CalcDigest(Digest.CellSpaceList(spacePool));

    public string CalcDigest(string spacesDigest)
    => "{" +
        $"vertexPool.Count: {vertexPool.Count}, " +
        $"boundaryPool digest: {Digest.CellBoundaryList(boundaryPool)}, " +
        $"spacePool digest: {spacesDigest}" +
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

    public static IndoorData? Deserialize(string json)
    {
        IndoorData? indoorData = JsonConvert.DeserializeObject<IndoorData>(json, new WKTConverter(), new CoorConverter());
        if (indoorData != null)
            indoorData.UpdateIndices();
        return indoorData;
    }

    public ICollection<Geometry> Polygonizer()
    {
        var polygonizer = new Polygonizer();
        polygonizer.Add(boundaryPool.Select(b => (Geometry)b.geom).ToList());
        return polygonizer.GetPolygons();
    }
}
