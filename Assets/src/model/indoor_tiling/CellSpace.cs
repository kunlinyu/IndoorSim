using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using UnityEngine;
#nullable enable
public class CellSpace
{
    [JsonPropertyAttribute] public Polygon Geom { get; private set; }
    [JsonPropertyAttribute] public List<CellVertex> shellVertices { get; private set; }
    [JsonPropertyAttribute] public List<CellBoundary> shellBoundaries { get; private set; }
    [JsonPropertyAttribute] public bool Navigable { get; set; } = false;
    [JsonIgnore]
    public List<CellVertex> allVertices
    {
        get
        {
            List<CellVertex> result = new List<CellVertex>(shellVertices);
            foreach (var hole in Holes)
                result.AddRange(hole.shellVertices);
            return result;
        }
    }
    [JsonIgnore]
    public List<CellBoundary> allBoundaries
    {
        get
        {
            List<CellBoundary> result = new List<CellBoundary>(shellBoundaries);
            foreach (var hole in Holes)
                result.AddRange(hole.shellBoundaries);
            return result;
        }
    }

    [JsonPropertyAttribute] public List<CellSpace> Holes { get; private set; } = new List<CellSpace>();
    [JsonIgnore] public Action OnUpdate = () => { };

    public CellSpace(Polygon polygon, ICollection<CellVertex> sortedVertices, ICollection<CellBoundary> boundaries)
    {
        Geom = polygon;
        shellVertices = new List<CellVertex>(sortedVertices);
        shellBoundaries = new List<CellBoundary>(boundaries);
    }

    public CellSpace ShellCellSpace()
    {
        return new CellSpace(new GeometryFactory().CreatePolygon(Geom.Shell), shellVertices, shellBoundaries);
    }

    public void UpdateFromVertex()
    {
        List<CellVertex> shellVertices2 = new List<CellVertex>(shellVertices);
        shellVertices2.Add(shellVertices.First());
        LinearRing shellRing = new GeometryFactory().CreateLinearRing(shellVertices2.Select(cv => cv.Coordinate).ToArray());

        List<LinearRing> holes = new List<LinearRing>();
        foreach (CellSpace hole in Holes)
        {
            List<CellVertex> shellOfHole = new List<CellVertex>(hole.shellVertices);
            shellOfHole.Add(hole.shellVertices.First());
            LinearRing holeRing = new GeometryFactory().CreateLinearRing(shellOfHole.Select(cv => cv.Coordinate).ToArray());
            holes.Add(holeRing);
            hole.UpdateFromVertex();
        }

        Geom = new GeometryFactory().CreatePolygon(shellRing, holes.ToArray());

        OnUpdate?.Invoke();
    }

    public void SplitBoundary(CellBoundary oldBoundary, CellBoundary newBoundary1, CellBoundary newBoundary2, CellVertex middleVertex)
    {
        HashSet<CellVertex> vertices = new HashSet<CellVertex>();
        vertices.Add(oldBoundary.P0);
        vertices.Add(oldBoundary.P1);
        vertices.Add(newBoundary1.P0);
        vertices.Add(newBoundary1.P1);
        vertices.Add(newBoundary2.P0);
        vertices.Add(newBoundary2.P1);
        vertices.Add(middleVertex);
        if (vertices.Count != 3)
            throw new ArgumentException("the old and new boundary don't connect as a triangle");

        CellSpace? target = null;
        if (shellBoundaries.Contains(oldBoundary))
            target = this;
        else
            target = Holes.FirstOrDefault(hole => hole.shellBoundaries.Contains(oldBoundary));
        if (target == null) throw new ArgumentException("can not find this boundary");

        // looking for index of two vertices in old boundary
        int index1 = 0;
        for (; index1 < target.shellVertices.Count; index1++)
            if (System.Object.ReferenceEquals(target.shellVertices[index1], oldBoundary.P0))
                break;
        if (index1 == target.shellVertices.Count) throw new ArgumentException("can not find vertices");
        int next = (index1 + 1) % target.shellVertices.Count;
        int prev = (index1 - 1) % target.shellVertices.Count;
        if (prev < 0) prev += target.shellVertices.Count;

        int index2 = 0;
        if (System.Object.ReferenceEquals(target.shellVertices[next], oldBoundary.P1))
            index2 = next;
        else if (System.Object.ReferenceEquals(target.shellVertices[prev], oldBoundary.P1))
            index2 = prev;
        else
            throw new ArgumentException("can not find the second vertices");

        // insert middleVertex into the middle of two old vertices
        if (Math.Abs(index1 - index2) == 1)
            target.shellVertices.Insert(Math.Max(index1, index2), middleVertex);
        else
            target.shellVertices.Add(middleVertex);


        // Handle boundary
        target.shellBoundaries.Remove(oldBoundary);
        target.shellBoundaries.Add(newBoundary1);
        target.shellBoundaries.Add(newBoundary2);
        UpdateFromVertex();
    }

    public static CellSpace MergeCellSpace(CellSpace cellSpace1, CellSpace cellSpace2)
    {
        // looking for nonCommonBoundaries
        HashSet<CellBoundary> commonBoundaries = new HashSet<CellBoundary>();
        foreach (var boundary1 in cellSpace1.shellBoundaries)
            foreach (var boundary2 in cellSpace2.shellBoundaries)
                if (System.Object.ReferenceEquals(boundary1, boundary2))
                    commonBoundaries.Add(boundary1);
        if (commonBoundaries.Count == 0)
            throw new ArgumentException("can not merget the two cellSpaces because they don't have common boundaries");

        HashSet<CellBoundary> nonCommonBoundaries = new HashSet<CellBoundary>();
        foreach (var b in cellSpace1.shellBoundaries)
            if (!commonBoundaries.Contains(b))
                nonCommonBoundaries.Add(b);
        foreach (var b in cellSpace2.shellBoundaries)
            if (!commonBoundaries.Contains(b))
                nonCommonBoundaries.Add(b);

        // Search for vertices in ring sequence from boundaries
        List<CellVertex> vertices = new List<CellVertex>();
        List<CellBoundary> waitingBoundaries = nonCommonBoundaries.ToList();

        CellBoundary currentBoundary = waitingBoundaries[0];
        CellVertex currentVertex = currentBoundary.P0;
        waitingBoundaries.Remove(currentBoundary);
        vertices.Add(currentVertex);
        do
        {
            currentVertex = currentBoundary.Another(currentVertex);
            currentBoundary = waitingBoundaries.FirstOrDefault(b => b.Contains(currentVertex));
            vertices.Add(currentVertex);
            waitingBoundaries.Remove(currentBoundary);
        } while (waitingBoundaries.Count > 0);

        // check vertices CCW
        List<CellVertex> tempRing = new List<CellVertex>(vertices);
        tempRing.Add(vertices[0]);
        if (!new GeometryFactory().CreateLinearRing(tempRing.Select(cv => cv.Coordinate).ToArray()).IsCCW)
            vertices.Reverse();

        Geometry unionPolygon = cellSpace1.Geom.Union(cellSpace2.Geom);
        if (unionPolygon.OgcGeometryType != OgcGeometryType.Polygon)
        {
            Debug.Log(unionPolygon.ToString());
            throw new Exception("Can not union two cellspace to be one polygon");
        }

        // Add merged hole
        return new CellSpace((Polygon)unionPolygon, vertices, nonCommonBoundaries);
    }

    public void AddHole(CellSpace cellSpace)
    {
        // Add hole into another hole, ignore
        foreach (var hole in Holes)
            if (hole.Geom.Contains(cellSpace.Geom))
                return;

        // new hole eat some hole
        List<CellSpace> independentHole = new List<CellSpace>();
        foreach (var hole in Holes)
            if (!cellSpace.Geom.Contains(hole.Geom))
                independentHole.Add(hole);
        Holes = independentHole;

        if (!Geom.Contains(cellSpace.Geom.Shell)) throw new ArgumentException("the polygon should contain the new hole.");


        Holes.Add(cellSpace.ShellCellSpace());

        HashSet<CellSpace> holesSet = new HashSet<CellSpace>(Holes);

        bool merge;
        do
        {
            merge = false;

            foreach (var hole1 in holesSet)
            {
                foreach (var hole2 in holesSet)
                    if (!System.Object.ReferenceEquals(hole1, hole2))  // generate pair of hole

                        foreach (var b1 in hole1.shellBoundaries)
                            foreach (var b2 in hole2.shellBoundaries)
                                if (System.Object.ReferenceEquals(b1, b2))  // adjacent holes have common boundary, lets merge them
                                {
                                    merge = true;
                                    holesSet.Remove(hole1);
                                    holesSet.Remove(hole2);
                                    holesSet.Add(MergeCellSpace(hole1, hole2));
                                    goto aftermerge;
                                }
            }

        aftermerge:;

        } while (merge);

        Holes.Clear();
        Holes.AddRange(holesSet);

        Geom = new GeometryFactory().CreatePolygon(Geom.Shell, Holes.Select(h => h.Geom.Shell).ToArray());
        OnUpdate?.Invoke();
    }

    public void Update()
    {
        // TODO: vertices to geom;
    }

}
