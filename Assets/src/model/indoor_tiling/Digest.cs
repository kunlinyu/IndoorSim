using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

public class Digest
{
    public static string Polygon(Polygon polygon)
        => "{" +
            $"Area:{polygon.Area.ToString("0.##########")}, " +
            $"Shell.Length:{polygon.Shell.Length}, " +
            $"Holes.Length:{polygon.Holes.Length}" +
            "}";

    public static string PolygonList(List<Polygon> polygons)
    {
        polygons.Sort((polygon1, polygon2) => Math.Sign(polygon1.Area - polygon2.Area));
        return "{" + String.Join(", ", polygons.Select(polygon => Digest.Polygon(polygon))) + "}";
    }

    public static string CellSpace(CellSpace space)
        => Digest.Polygon(space.Geom);

    public static string CellBoundaryList(List<CellBoundary> boundaries)
    {
        boundaries.Sort((b1, b2) => Math.Sign(b1.Geom.Length - b2.Geom.Length));
        return "{" + String.Join(", ", boundaries.Select(b => $"{b.Geom.Length}")) + "}";
    }

    public static string CellSpaceList(List<CellSpace> spaces)
    {
        spaces.Sort((space1, space2) => Math.Sign(space1.Geom.Area - space2.Geom.Area));
        return "{" + String.Join(", ", spaces.Select(space => Digest.CellSpace(space))) + "}";
    }
}
