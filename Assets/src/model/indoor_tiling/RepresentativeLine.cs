using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
#nullable enable

public enum PassType
{
    IllForm,  // the "from" is not the inbound of "through", or the "to" is not the outbound of "through"
    DoNotPass,
    AllowedToPass,
}
public class RepresentativeLine
{
    [JsonPropertyAttribute] public LineString geom { get; private set; }
    [JsonPropertyAttribute] public CellBoundary from { get; private set; }
    [JsonPropertyAttribute] public CellBoundary to { get; private set; }
    [JsonPropertyAttribute] public CellSpace through { get; private set; }
    [JsonPropertyAttribute] public PassType passType { get; private set; }

    public RepresentativeLine(CellBoundary from, CellBoundary to, CellSpace through, PassType passType)
    {
        this.from = from;
        this.to = to;
        this.through = through;
        this.passType = passType;

        if (!through.allBoundaries.Contains(from)) throw new ArgumentException("the \"from\" boundary should bound the \"through\" space");
        if (!through.allBoundaries.Contains(to)) throw new ArgumentException("the \"to\" boundary should bound the \"through\" space");

        geom = UpdateGeom();
    }

    public LineString UpdateGeom()
    {
        double bazierHandlerLength = 0.3f;
        var gf = new GeometryFactory();

        Coordinate P0 = from.Geom.Centroid.Coordinate;
        Coordinate P1;
        Coordinate P2;
        Coordinate P3 = to.Geom.Centroid.Coordinate;

        if (from.leftSpace == through)
        {
            Coordinate fromP0left = M.Rotate(from.P1.Coordinate, from.P0.Coordinate, Math.PI / 2.0f);
            P1 = M.Translate(from.Geom.Centroid.Coordinate, from.P0.Coordinate, fromP0left, bazierHandlerLength);
        }
        else if (from.rightSpace == through)
        {
            Coordinate fromP0right = M.Rotate(from.P1.Coordinate, from.P0.Coordinate, -Math.PI / 2.0f);
            P1 = M.Translate(from.Geom.Centroid.Coordinate, from.P0.Coordinate, fromP0right, bazierHandlerLength);
        }
        else throw new Exception("space contain the boundary but it is neighter the left nor the right side of boundary");

        if (to.leftSpace == through)
        {
            Coordinate toP0left = M.Rotate(to.P1.Coordinate, to.P0.Coordinate, Math.PI / 2.0f);
            P2 = M.Translate(to.Geom.Centroid.Coordinate, to.P0.Coordinate, toP0left, bazierHandlerLength);
        }
        else if (to.rightSpace == through)
        {
            Coordinate toP0right = M.Rotate(to.P1.Coordinate, to.P0.Coordinate, -Math.PI / 2.0f);
            P2 = M.Translate(to.Geom.Centroid.Coordinate, to.P0.Coordinate, toP0right, bazierHandlerLength);
        }
        else throw new Exception("space contain the boundary but it is neighter the left nor the right side of boundary");


        geom = gf.CreateLineString(M.bazierCurve4(P0, P1, P2, P3, 20));
        return geom;
    }
}