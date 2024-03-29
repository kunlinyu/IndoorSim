using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Triangulate;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public class U
{
    static public Mesh? TriangulatePolygon2Mesh(in Polygon polygon)
    {
        if (polygon == null) return null;

        Vector3[] triVertices;
        int[] triIndices;
        int[] lineIndices;
        if (PolygonIsSimple(polygon))
            U.TriangulateSimplePolygon(polygon, out triVertices, out triIndices, out lineIndices);
        else
            U.TriangulatePolygon(polygon, out triVertices, out triIndices, out lineIndices);

        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.subMeshCount = 2;
        mesh.SetVertices(triVertices);
        mesh.SetIndices(triIndices, MeshTopology.Triangles, 0);
        mesh.SetIndices(lineIndices, MeshTopology.Lines, 1);

        Vector2[] uv = new Vector2[triVertices.Length];
        for (int i = 0; i < uv.Length; i++)
            uv[i] = new Vector2(triVertices[i].x, triVertices[i].z);
        mesh.SetUVs(0, uv);

        return mesh;
    }

    static public Mesh? MergeMesh(Mesh mesh1, Mesh mesh2)
    {
        if (mesh1 == null) return mesh2;
        if (mesh2 == null) return mesh1;
        Mesh result = new()
        {
            subMeshCount = mesh1.subMeshCount + mesh2.subMeshCount
        };

        result.SetVertices(mesh1.vertices.Concat(mesh2.vertices).ToArray());

        for (int i = 0; i < mesh1.subMeshCount; i++)
            result.SetIndices(mesh1.GetIndices(i), mesh1.GetTopology(i), i);
        for (int i = 0; i < mesh2.subMeshCount; i++)
        {
            int[] indices2 = mesh2.GetIndices(i).Select(index => index + mesh1.vertices.Length).ToArray();
            result.SetIndices(indices2, mesh2.GetTopology(i), i + mesh1.subMeshCount);
        }

        return result;
    }

    static public bool PolygonIsSimple(Polygon polygon)
    {
        if (polygon.NumPoints != 5)
            return false;

        var coors = polygon.Coordinates;
        var ls1 = new LineSegment(coors[0], coors[2]);
        var ls2 = new LineSegment(coors[1], coors[3]);
        if (ls1.Intersection(ls2) == null)
            return false;

        return true;
    }

    static public void TriangulateSimplePolygon(in Polygon polygon, out Vector3[] triVertices, out int[] triIndices, out int[] lineIndices)
    {
        var coors = polygon.Coordinates;

        triVertices = new Vector3[4];
        triVertices[0] = Coor2Vec(coors[0]);
        triVertices[1] = Coor2Vec(coors[1]);
        triVertices[2] = Coor2Vec(coors[2]);
        triVertices[3] = Coor2Vec(coors[3]);

        triIndices = new int[6];
        triIndices[0] = 0;
        triIndices[1] = 2;
        triIndices[2] = 1;
        triIndices[3] = 0;
        triIndices[4] = 3;
        triIndices[5] = 2;

        lineIndices = new int[2];
        lineIndices[0] = 0;
        lineIndices[1] = 2;
    }

    static public void TriangulatePolygon(in Polygon polygon, out Vector3[] triVertices, out int[] triIndices, out int[] lineIndices)
    {
        // if (!polygon.IsSimple || !polygon.ExteriorRing.IsRing)
        // {
        //     Debug.Log(polygon.ToText());
        //     throw new ArgumentException("polygon is not simple");
        // }

        var triBuilder = new ConformingDelaunayTriangulationBuilder();
        triBuilder.SetSites(polygon);
        triBuilder.Constraints = polygon;
        triBuilder.Tolerance = 1e-3;
        GeometryCollection result;

        try
        {
            result = triBuilder.GetTriangles(new GeometryFactory());
        }
        catch (Exception e)
        {
            Debug.Log("Conforming Delaunay Triangulation Error!!!");
            Debug.Log(e.Message);
            Debug.Log(polygon.ToText());
            throw e;
        }

        List<Geometry> insideResult = new List<Geometry>();
        foreach (Geometry geom in result.Geometries)
        {
            var centroid = geom.Centroid;
            if (polygon.EnvelopeInternal.Contains(centroid.Coordinate))
                if (polygon.Contains(centroid))
                    insideResult.Add(geom);
        }

        triVertices = new Vector3[insideResult.Count * 3];
        triIndices = new int[insideResult.Count * 3];
        lineIndices = new int[insideResult.Count * 6];

        int i = 0;
        foreach (Geometry geom in insideResult)
        {
            // the sequence of coordinates of shapes in NTS/JTS is anti-clock-wise
            // the sequence of coordinates of mesh in Unity is clock-wise
            // So we should assign 2,1,0, to 0,1,2 to reverse
            triVertices[i * 3 + 0] = Coor2Vec(geom.Coordinates[2]);
            triVertices[i * 3 + 1] = Coor2Vec(geom.Coordinates[1]);
            triVertices[i * 3 + 2] = Coor2Vec(geom.Coordinates[0]);


            // indices if triangle just same as his own indices
            triIndices[i * 3 + 0] = i * 3 + 0;
            triIndices[i * 3 + 1] = i * 3 + 1;
            triIndices[i * 3 + 2] = i * 3 + 2;

            // each triangle have 3 lines and 6 coordinates
            lineIndices[i * 6 + 0] = i * 3 + 0;
            lineIndices[i * 6 + 1] = i * 3 + 1;
            lineIndices[i * 6 + 2] = i * 3 + 1;
            lineIndices[i * 6 + 3] = i * 3 + 2;
            lineIndices[i * 6 + 4] = i * 3 + 2;
            lineIndices[i * 6 + 5] = i * 3 + 0;

            i++;
        }
    }
    static public Vector3 Coor2Vec(Coordinate coordinate) => new Vector3((float)coordinate.X, 0.0f, (float)coordinate.Y);
    static public Vector3 Point2Vec(Point point) => Coor2Vec(point.Coordinate);
    static public Coordinate Vec2Coor(Vector3 vec) => new Coordinate(vec.x, vec.z);
    static public Coordinate? Vec2Coor(Vector3? vec) => vec == null ? null : new Coordinate(vec!.Value.x, vec!.Value.z);
    static public Point Vec2Point(Vector3 vec) => new Point(Vec2Coor(vec));

    static public Vector3 Coor2Screen(Coordinate coor) => Camera.main.WorldToScreenPoint(U.Coor2Vec(coor));

}
