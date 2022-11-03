using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

public enum PassType
{
    AllowedToPass = 0,
    DoNotPass = 1,
}
public class RepresentativeLine
{
    [JsonPropertyAttribute] public CellBoundary fr { get; private set; }
    [JsonPropertyAttribute] public CellBoundary to { get; private set; }
    [JsonPropertyAttribute] public PassType pass { get; set; }
    [JsonIgnore] public LineString? geom { get; private set; }

    public bool IllForm(CellSpace through)
    => (through.navigable != Navigable.Navigable) ||
       (!through.InBound().Contains(fr) && !through.StopBound().Contains(fr)) ||
       (!through.OutBound().Contains(to) && !through.StopBound().Contains(to)) ||
       (through.StopBound().Contains(fr) && through.StopBound().Contains(to));

#pragma warning disable CS8618
    public RepresentativeLine() { }  // for deserialize only
#pragma warning restore CS8618

    public RepresentativeLine(CellBoundary fr, CellBoundary to, PassType passType)
    {
        this.fr = fr;
        this.to = to;
        this.pass = passType;
    }

    public RepresentativeLine(CellBoundary fr, CellBoundary to, CellSpace through, PassType passType)
    {
        this.fr = fr;
        this.to = to;
        this.pass = passType;

        if (!through.allBoundaries.Contains(fr)) throw new ArgumentException("the \"fr\" boundary should bound the \"through\" space");
        if (!through.allBoundaries.Contains(to)) throw new ArgumentException("the \"to\" boundary should bound the \"through\" space");
    }

    public LineString? UpdateGeom(CellSpace through)
    {
        if (IllForm(through)) return null;
        Coordinate frCentroid = fr.geom.Centroid.Coordinate;
        Coordinate toCentroid = to.geom.Centroid.Coordinate;
        double lengthRoughEstimate = frCentroid.Distance(toCentroid);
        double bazierHandlerLength = 0.2d * lengthRoughEstimate;
        double shiftRatio = 0.01f;

        Coordinate P0;
        Coordinate P1;
        Coordinate P2;
        Coordinate P3;

        double fromShift = shiftRatio * fr.geom.Length;
        if (fr.leftSpace == through)
        {
            P0 = M.Translate(frCentroid, fr.P0.Coordinate, fr.P1.Coordinate, fromShift);
            Coordinate fromP0left = M.Rotate(fr.P1.Coordinate, fr.P0.Coordinate, Math.PI / 2.0f);
            P1 = M.Translate(P0, fr.P0.Coordinate, fromP0left, bazierHandlerLength);
        }
        else if (fr.rightSpace == through)
        {
            P0 = M.Translate(frCentroid, fr.P1.Coordinate, fr.P0.Coordinate, fromShift);
            Coordinate fromP0right = M.Rotate(fr.P1.Coordinate, fr.P0.Coordinate, -Math.PI / 2.0f);
            P1 = M.Translate(P0, fr.P0.Coordinate, fromP0right, bazierHandlerLength);
        }
        else throw new Exception($"space({through.Id}) contain the boundary({fr.Id}) but it is neither the left nor the right side of boundary");

        double toShift = shiftRatio * to.geom.Length;
        if (to.leftSpace == through)
        {
            P3 = M.Translate(toCentroid, to.P1.Coordinate, to.P0.Coordinate, toShift);
            Coordinate toP0left = M.Rotate(to.P1.Coordinate, to.P0.Coordinate, Math.PI / 2.0f);
            P2 = M.Translate(P3, to.P0.Coordinate, toP0left, bazierHandlerLength);
        }
        else if (to.rightSpace == through)
        {
            P3 = M.Translate(toCentroid, to.P0.Coordinate, to.P1.Coordinate, toShift);
            Coordinate toP0right = M.Rotate(to.P1.Coordinate, to.P0.Coordinate, -Math.PI / 2.0f);
            P2 = M.Translate(P3, to.P0.Coordinate, toP0right, bazierHandlerLength);
        }
        else throw new Exception($"space({through.Id}) contain the boundary({to.Id}) but it is neither the left nor the right side of boundary");


        geom = new LineString(M.BazierCurve4(P0, P1, P2, P3, 10));
        return geom;
    }
}