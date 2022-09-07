using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable

public class CellBoundary
{
    [JsonPropertyAttribute] public string Id { get; set; }
    [JsonPropertyAttribute] public CellVertex P0 { get; private set; }
    [JsonPropertyAttribute] public CellVertex P1 { get; private set; }
    [JsonPropertyAttribute] private LineString? Geom;
    [JsonIgnore] public CellSpace? leftSpace;
    [JsonIgnore] public CellSpace? rightSpace;
    [JsonPropertyAttribute] private NaviDirection naviDir { set; get; } = NaviDirection.Left2Right;
    [JsonPropertyAttribute] private Navigable navigable { set; get; } = Navigable.Navigable;

    [JsonIgnore] private LineString? autoGenGeom;

    [JsonIgnore]
    public LineString geom
    {
        get
        {
            if (Geom != null) return Geom;
            if (autoGenGeom == null)
                autoGenGeom = new GeometryFactory().CreateLineString(new Coordinate[] { P0.Coordinate, P1.Coordinate });
            return autoGenGeom;
        }
    }

    [JsonIgnore] public NaviDirection NaviDir { get => naviDir; set { naviDir = value; OnUpdate?.Invoke(); } }
    [JsonIgnore] public Navigable Navigable { get => navigable; set { navigable = value; OnUpdate?.Invoke(); } }
    [JsonIgnore] public Action OnUpdate = () => { };
    [JsonIgnore] public LineString GeomReverse { get => (LineString)geom.Reverse(); }

    private CellBoundary()  // for deserialization only
    {
        Id = "";
        autoGenGeom = new LineString(new Coordinate[0]);
        P0 = new CellVertex();
        P1 = new CellVertex();
    }

    public Navigable SmartNavigable()
    {
        if (leftSpace == null || rightSpace == null)
            return global::Navigable.PhysicallyNonNavigable;
        else if (navigable == global::Navigable.PhysicallyNonNavigable ||
                 leftSpace.Navigable == global::Navigable.PhysicallyNonNavigable ||
                 rightSpace.Navigable == global::Navigable.PhysicallyNonNavigable)
            return global::Navigable.PhysicallyNonNavigable;
        else if (navigable == global::Navigable.LogicallyNonNavigable ||
                 leftSpace.Navigable == global::Navigable.LogicallyNonNavigable ||
                 rightSpace.Navigable == global::Navigable.LogicallyNonNavigable)
            return global::Navigable.LogicallyNonNavigable;
        else
            return global::Navigable.Navigable;
    }

    public List<CellSpace> Spaces()
    {
        List<CellSpace> result = new List<CellSpace>();
        if (leftSpace != null)
            result.Add(leftSpace);
        if (rightSpace != null)
            result.Add(rightSpace);
        return result;
    }

    public LineString GeomOrder(CellVertex start, CellVertex end)
    {
        if (start == P0 && end == P1) return this.geom;
        if (start == P1 && end == P0) return this.GeomReverse;
        throw new ArgumentException("Don't contain vertices");
    }

    public LineString GeomEndWith(CellVertex end)
    {
        if (end == P1) return this.geom;
        if (end == P0) return this.GeomReverse;
        throw new ArgumentException("Don't contain end vertex");
    }

    public CellBoundary(CellVertex p0, CellVertex p1, string id = "null")
    {
        if (p0 == p1) throw new ArgumentException("CellBoundary can not connect one same CellVertex");
        autoGenGeom = new GeometryFactory().CreateLineString(new Coordinate[] { p0.Coordinate, p1.Coordinate });
        P0 = p0;
        P1 = p1;
        Id = id;
    }

    public void UpdateFromVertex()
    {
        Coordinate[] coor = geom.Coordinates;
        coor[0] = P0.Coordinate;
        coor[coor.Length - 1] = P1.Coordinate;
        autoGenGeom = new GeometryFactory().CreateLineString(coor);
        OnUpdate?.Invoke();
    }

    public void UpdateGeom(LineString ls)
    {
        if (ls.StartPoint.Distance(P0.Geom) > 1e-4f) throw new ArgumentException("the geom of boundary should connect vertices.");
        if (ls.EndPoint.Distance(P1.Geom) > 1e-4f) throw new ArgumentException("the geom of boundary should connect vertices.");
        if (ls.NumPoints > 2)
            Geom = ls;
        else
            autoGenGeom = ls;
        OnUpdate?.Invoke();
    }

    public bool Contains(CellVertex cv)
    {
        if (cv == P0) return true;
        if (cv == P1) return true;
        return false;
    }

    public CellVertex Another(CellVertex one)
    {
        if (one == P0) return P1;
        if (one == P1) return P0;
        throw new ArgumentException("Not any one of my CellVertex");
    }

    public CellSpace? Another(CellSpace space)
    {
        if (space == leftSpace) return rightSpace;
        if (space == rightSpace) return leftSpace;
        return null;
    }

    public bool ConnectSameVertices(CellBoundary cb)
    {
        if (P0 == cb.P0 && P1 == cb.P1) return true;
        if (P0 == cb.P1 && P1 == cb.P0) return true;
        return false;
    }

    public Point ClosestPointTo(CellVertex cv)
    {
        if (cv == P0) return geom.GetPointN(1);
        if (cv == P1) return geom.GetPointN(geom.NumPoints - 2);
        throw new ArgumentException("Not any one of my CellVertex");
    }

    public void PartialBound(CellSpace space)
    {
        if (!space.allBoundaries.Contains(this)) throw new ArgumentException($"the space({space.Id}) don't contain this boundary({Id})");

        bool partOfShell = true;
        if (!space.shellBoundaries.Contains(this))
            partOfShell = false;

        CellSpace? target = null;
        if (!partOfShell)  // looking for the hole contain the boundary
            target = space.Holes.FirstOrDefault(hole => hole.shellBoundaries.Contains(this));
        else
            target = space;
        if (target == null) throw new Exception("neither shell nor hole contain this boundary");

        int P0Index = target.shellVertices.FindIndex(0, target.shellVertices.Count - 1, cv => cv == P0);
        int P1Index = target.shellVertices.FindIndex(0, target.shellVertices.Count - 1, cv => cv == P1);

        bool leftSide = P0Index < P1Index;
        if (Math.Abs(P0Index - P1Index) != 1)
            leftSide = !leftSide;
        if (!partOfShell)
            leftSide = !leftSide;

        if (leftSide)
            leftSpace = space;
        else
            rightSpace = space;
        OnUpdate?.Invoke();
    }

    public void PartialUnBound(CellSpace space)
    {
        if (leftSpace == space)
            leftSpace = null;
        else if (rightSpace == space)
            rightSpace = null;
        OnUpdate?.Invoke();
    }

    public void PartialUnBoundAll()
    {
        leftSpace = null;
        rightSpace = null;
        OnUpdate?.Invoke();
    }
}
