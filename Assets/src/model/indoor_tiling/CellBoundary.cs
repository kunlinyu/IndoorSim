using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

struct BoundaryType
{

}
public class CellBoundary
{
    [JsonPropertyAttribute] public string Id { get; private set; }
    [JsonPropertyAttribute] public LineString Geom { get; private set; }
    [JsonPropertyAttribute] public CellVertex P0 { get; private set; }
    [JsonPropertyAttribute] public CellVertex P1 { get; private set; }
    // [JsonPropertyAttribute] public CellSpace? LeftSpace { get; set; }
    // [JsonPropertyAttribute] public CellSpace? RightSpace { get; set; }

    //      P1
    //      ^
    //      |
    // left | right
    //      |
    //      P0

    // navigable: at least one of two boolean variables below are true
    // non-navigable: both of these two boolean variables below are false
    [JsonPropertyAttribute] public bool Right2Left { get; set; }= true;
    [JsonPropertyAttribute] public bool Left2Right { get; set; } = true;

    // left/right Functional == true means agents may stop at the left/right side of this boundary to do something
    [JsonPropertyAttribute] public bool LeftFunctional { get; set; } = false;
    [JsonPropertyAttribute] public bool RightFunctional { get; set; }= false;
    [JsonIgnore] public Action OnUpdate = () => {};

    [JsonIgnore] public LineString GeomReverse { get => (LineString)Geom.Reverse(); }

    public LineString GeomOrder(CellVertex start, CellVertex end)
    {
        if (Object.ReferenceEquals(start, P0) && Object.ReferenceEquals(end, P1)) return this.Geom;
        if (Object.ReferenceEquals(start, P1) && Object.ReferenceEquals(end, P0)) return this.GeomReverse;
        throw new ArgumentException("Don't contain vertices");
    }

    public LineString GeomEndWith(CellVertex end)
    {
        if (Object.ReferenceEquals(end, P1)) return this.Geom;
        if (Object.ReferenceEquals(end, P0)) return this.GeomReverse;
        throw new ArgumentException("Don't contain end vertex");
    }

    public CellBoundary(LineString ls, CellVertex p0, CellVertex p1, string id)
    {
        if (Object.ReferenceEquals(p0, p1)) throw new ArgumentException("CellBoundary can not connect one same CellVertex");
        if (ls.NumPoints < 2) throw new ArgumentException("line string of boundary should have 2 points at least");
        Geom = ls;
        P0 = p0;
        P1 = p1;
        Id = id;
    }

    public CellBoundary(CellVertex p0, CellVertex p1, string id = "null")
    {
        if (Object.ReferenceEquals(p0, p1)) throw new ArgumentException("CellBoundary can not connect one same CellVertex");
        Geom = new GeometryFactory().CreateLineString(new Coordinate[] { p0.Coordinate, p1.Coordinate});
        P0 = p0;
        P1 = p1;
        Id = id;
    }

    public void UpdateFromVertex()
    {
        Coordinate[] coor = Geom.Coordinates;
        coor[0] = P0.Coordinate;
        coor[coor.Length - 1] = P1.Coordinate;
        Geom = new GeometryFactory().CreateLineString(coor);
        OnUpdate?.Invoke();
    }

    public bool Contains(CellVertex cv)
    {
        if (Object.ReferenceEquals(cv, P0)) return true;
        if (Object.ReferenceEquals(cv, P1)) return true;
        return false;
    }

    public CellVertex Another(CellVertex one)
    {
        if (Object.ReferenceEquals(one, P0)) return P1;
        if (Object.ReferenceEquals(one, P1)) return P0;
        throw new ArgumentException("Not any one of my CellVertex");
    }

    public bool ConnectSameVertices(CellBoundary cb)
    {
        if (Object.ReferenceEquals(P0, cb.P0) && Object.ReferenceEquals(P1, cb.P1)) return true;
        if (Object.ReferenceEquals(P0, cb.P1) && Object.ReferenceEquals(P1, cb.P0)) return true;
        return false;
    }

    public Point ClosestPointTo(CellVertex cv)
    {
        if (Object.ReferenceEquals(cv, P0)) return Geom.GetPointN(1);
        if (Object.ReferenceEquals(cv, P1)) return Geom.GetPointN(Geom.NumPoints - 2);
        throw new ArgumentException("Not any one of my CellVertex");
    }
}
