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
            List<CellVertex> result = new List<CellVertex>();
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
            List<CellBoundary> result = new List<CellBoundary>();
            foreach (var hole in Holes)
                result.AddRange(hole.shellBoundaries);
            return result;
        }
    }

    [JsonPropertyAttribute] public List<CellSpace> Holes { get; private set; } = new List<CellSpace>();
    [JsonIgnore] public Action OnUpdate = () => { };

    public CellSpace(Polygon polygon, ICollection<CellVertex> vertices, ICollection<CellBoundary> boundaries)
    {
        Geom = polygon;
        shellVertices = new List<CellVertex>(vertices);
        shellBoundaries = new List<CellBoundary>(boundaries);
    }

    public CellSpace ShellCellSpace()
    {
        return new CellSpace(new GeometryFactory().CreatePolygon(Geom.Shell), shellVertices, shellBoundaries);
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
                foreach (var hole2 in holesSet)
                    if (!System.Object.ReferenceEquals(hole1, hole2))  // generate pair of hole

                        foreach (var b1 in hole1.shellBoundaries)
                            foreach (var b2 in hole2.shellBoundaries)
                                if (System.Object.ReferenceEquals(b1, b2))  // adjacent holes have common boundary, lets merge them
                                {
                                    merge = true;

                                    // remove them
                                    holesSet.Remove(hole1);
                                    holesSet.Remove(hole2);

                                    // looking for nonCommonBoundaries
                                    HashSet<CellBoundary> commonBoundaries = new HashSet<CellBoundary>();
                                    HashSet<CellBoundary> nonCommonBoundaries = new HashSet<CellBoundary>();
                                    foreach (var boundary1 in hole1.shellBoundaries)
                                        foreach (var boundary2 in hole2.shellBoundaries)
                                            if (System.Object.ReferenceEquals(boundary1, boundary2))
                                                commonBoundaries.Add(boundary1);
                                    foreach (var b in hole1.shellBoundaries)
                                        if (!commonBoundaries.Contains(b))
                                            nonCommonBoundaries.Add(b);
                                    foreach (var b in hole2.shellBoundaries)
                                        if (!commonBoundaries.Contains(b))
                                            nonCommonBoundaries.Add(b);

                                    // looking for vertices
                                    HashSet<CellVertex> tempVertices = new HashSet<CellVertex>();
                                    HashSet<CellVertex> tobeRemoveVertices = new HashSet<CellVertex>();
                                    foreach (var b in commonBoundaries)
                                    {
                                        if (tempVertices.Contains(b.P0))
                                            tobeRemoveVertices.Add(b.P0);
                                        else
                                            tempVertices.Add(b.P0);
                                        if (tempVertices.Contains(b.P1))
                                            tobeRemoveVertices.Add(b.P1);
                                        else
                                            tempVertices.Add(b.P1);
                                    }
                                    List<CellVertex> remainVertices = new List<CellVertex>();
                                    foreach (var v in hole1.shellVertices)
                                        if (!tobeRemoveVertices.Contains(v))
                                            remainVertices.Add(v);
                                    foreach (var v in hole2.shellVertices)
                                        if (!tobeRemoveVertices.Contains(v))
                                            remainVertices.Add(v);

                                    // Add merged hole
                                    CellSpace newHole = new CellSpace((Polygon)hole1.Geom.Union(hole2.Geom), remainVertices, nonCommonBoundaries);
                                    holesSet.Add(newHole);

                                    goto aftermerge;
                                }

                            aftermerge:;

        } while (merge);

        Holes.Clear();
        Holes.AddRange(holesSet);

        Geom = new GeometryFactory().CreatePolygon(Geom.Shell, Holes.Select(h => h.Geom.Shell).ToArray());
    }

    public void Update()
    {
        // TODO: vertices to geom;
    }

}
