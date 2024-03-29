using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

#nullable enable


public class CellSpace : Container
{
    [JsonIgnore] public string Id { get; set; } = "";
    [JsonPropertyAttribute] public string? level = null;
    [JsonPropertyAttribute] public string? name = null;
    [JsonPropertyAttribute] public bool PoI = false;
    [JsonPropertyAttribute] public List<CellVertex> shellVertices { get; private set; } = new List<CellVertex>();
    [JsonPropertyAttribute] public List<CellBoundary> shellBoundaries { get; private set; } = new List<CellBoundary>();
    [JsonPropertyAttribute] public List<CellSpace> Holes { get; private set; } = new List<CellSpace>();
    [JsonIgnore] public RLineGroup? rLines;
    [JsonIgnore] public Polygon Polygon { get => (Polygon)Geom!; }

    [JsonIgnore]
    public List<CellBoundary> allBoundaries
    {
        get
        {
            HashSet<CellBoundary> result = new HashSet<CellBoundary>(shellBoundaries);
            foreach (var hole in Holes)
                result.UnionWith(hole.shellBoundaries);
            return result.ToList();
        }
    }

    [JsonIgnore]
    public Navigable Navigable
    {
        get => navigable;
        set
        {
            navigable = value;
            allBoundaries.ForEach(b => b.OnUpdate?.Invoke());
            OnUpdate?.Invoke();
        }
    }

    [JsonIgnore]
    public List<CellVertex> allVertices
    {
        get
        {
            HashSet<CellVertex> result = new HashSet<CellVertex>(shellVertices);
            foreach (var hole in Holes)
                result.UnionWith(hole.shellVertices);
            return result.ToList();
        }
    }

    [JsonIgnore] public Action OnUpdate = () => { };

    private CellSpace() : base("")
    {
        Geom = new GeometryFactory().CreatePolygon();
    }

    [OnDeserialized] internal void OnDeserializedMethod(StreamingContext context) => UpdateFromVertex();
    [OnSerializing] private void OnSerializingMethod(StreamingContext context) => Geom = null;
    [OnSerialized] private void OnSerializedMethod(StreamingContext context) => UpdateFromVertex();  // TODO(performance optimization): too heavy to re update for serialization

    public CellSpace(Polygon polygon, ICollection<CellVertex> sortedVertices, ICollection<CellBoundary> boundaries, string id = "") : base(id)
    {
        Geom = polygon;
        shellVertices = new List<CellVertex>(sortedVertices);
        shellBoundaries = new List<CellBoundary>(boundaries);
    }

    public CellSpace(ICollection<CellVertex> sortedVertices, ICollection<CellBoundary> boundaries, string id = "") : base(id)
    {
        shellVertices = new List<CellVertex>(sortedVertices);
        shellBoundaries = new List<CellBoundary>(boundaries);
        UpdateFromVertex();
    }

    public CellSpace ShellCellSpace()
    {
        return new CellSpace(new GeometryFactory().CreatePolygon(Polygon.Shell), shellVertices, shellBoundaries, "shell cell space");
    }

    // TODO(to support future feature): we should use UpdateFromBoundary to support complex boundary geometry
    public bool UpdateFromVertex()
    {
        List<CellVertex> shellVertices2 = new(shellVertices) { shellVertices.First() };
        LinearRing shellRing = new GeometryFactory().CreateLinearRing(shellVertices2.Select(cv => cv.Coordinate).ToArray());

        Holes.ForEach(hole => hole.UpdateFromVertex());

        Polygon tempPolygon = new GeometryFactory().CreatePolygon(shellRing);
        if (Holes.Any(hole => !tempPolygon.Contains(hole.Geom)))
            return false;

        Geom = new GeometryFactory().CreatePolygon(shellRing, Holes.Select(h => h.Polygon.Shell).ToArray());

        return true;
    }

    public void SplitBoundary(CellBoundary oldBoundary, CellBoundary newBoundary1, CellBoundary newBoundary2, CellVertex middleVertex)
    {
        HashSet<CellVertex> vertices = new()
        {
            oldBoundary.P0,
            oldBoundary.P1,
            newBoundary1.P0,
            newBoundary1.P1,
            newBoundary2.P0,
            newBoundary2.P1,
            middleVertex
        };
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
            if (ReferenceEquals(target.shellVertices[index1], oldBoundary.P0))
                break;
        if (index1 == target.shellVertices.Count) throw new ArgumentException("can not find vertices");
        int next = (index1 + 1) % target.shellVertices.Count;
        int prev = (index1 - 1) % target.shellVertices.Count;
        if (prev < 0) prev += target.shellVertices.Count;

        int index2 = 0;
        if (ReferenceEquals(target.shellVertices[next], oldBoundary.P1))
            index2 = next;
        else if (ReferenceEquals(target.shellVertices[prev], oldBoundary.P1))
            index2 = prev;
        else
            throw new ArgumentException("can not find the second vertices");

        // insert middleVertex into the middle of two old vertices
        if (Math.Abs(index1 - index2) == 1)
            target.shellVertices.Insert(Math.Max(index1, index2), middleVertex);
        else
            target.shellVertices.Add(middleVertex);


        // Replace Boundaries
        target.shellBoundaries.Remove(oldBoundary);
        target.shellBoundaries.Add(newBoundary1);
        target.shellBoundaries.Add(newBoundary2);

        UpdateFromVertex();
    }

    public void MergeBoundary(CellBoundary oldBoundary1, CellBoundary oldBoundary2, CellBoundary newBoundary, CellVertex middleVertex)
    {
        HashSet<CellVertex> vertices = new()
        {
            newBoundary.P0,
            newBoundary.P1,
            oldBoundary1.P0,
            oldBoundary1.P1,
            oldBoundary2.P0,
            oldBoundary2.P1,
            middleVertex
        };
        if (vertices.Count != 3)
            throw new ArgumentException("the old and new boundary don't connect as a triangle");

        CellSpace? target = null;
        if (shellBoundaries.Contains(oldBoundary1))
            target = this;
        else
            target = Holes.FirstOrDefault(hole => hole.shellBoundaries.Contains(oldBoundary1));
        if (target == null) throw new ArgumentException("can not find this boundary");
        if (!target.shellBoundaries.Contains(oldBoundary2)) throw new ArgumentException("the two old boundaries don't belong to same space");

        // Remove vertex
        target.shellVertices.Remove(middleVertex);

        // Replace Boundaries
        target.shellBoundaries.Remove(oldBoundary1);
        target.shellBoundaries.Remove(oldBoundary2);
        target.shellBoundaries.Add(newBoundary);

        UpdateFromVertex();
    }

    public static CellSpace Merge(CellSpace cellSpace1, CellSpace cellSpace2)
    {
        // If the two arguments touches with each other, this function will merge them.

        // looking for nonCommonBoundaries
        HashSet<CellBoundary> commonBoundaries = new HashSet<CellBoundary>();
        foreach (var boundary1 in cellSpace1.shellBoundaries)
            foreach (var boundary2 in cellSpace2.shellBoundaries)
                if (System.Object.ReferenceEquals(boundary1, boundary2))
                    commonBoundaries.Add(boundary1);
        if (commonBoundaries.Count == 0)
            throw new ArgumentException("can not merge or minus the two cellSpaces because they don't have common boundaries");

        HashSet<CellBoundary> nonCommonBoundaries = new HashSet<CellBoundary>();
        foreach (var b in cellSpace1.shellBoundaries)
            if (!commonBoundaries.Contains(b))
                nonCommonBoundaries.Add(b);
        foreach (var b in cellSpace2.shellBoundaries)
            if (!commonBoundaries.Contains(b))
                nonCommonBoundaries.Add(b);

        // Search for vertices in ring sequence from boundaries
        List<List<CellVertex>> vertices = new List<List<CellVertex>>();
        List<List<CellBoundary>> boundaries = new List<List<CellBoundary>>();

        List<CellBoundary> waitingBoundaries = nonCommonBoundaries.ToList();

        int ringIndex = 0;
        do
        {
            vertices.Add(new List<CellVertex>());
            boundaries.Add(new List<CellBoundary>());

            CellBoundary? currentBoundary = waitingBoundaries[0];
            waitingBoundaries.Remove(currentBoundary);
            boundaries[ringIndex].Add(currentBoundary);

            CellVertex currentVertex = currentBoundary.P0;
            vertices[ringIndex].Add(currentVertex);
            Console.WriteLine(ringIndex);
            do
            {
                currentVertex = currentBoundary!.Another(currentVertex);
                vertices[ringIndex].Add(currentVertex);

                currentBoundary = waitingBoundaries.FirstOrDefault(b => b.Contains(currentVertex));
                waitingBoundaries.Remove(currentBoundary);
                if (currentBoundary != null)
                    boundaries[ringIndex].Add(currentBoundary);
            } while (currentBoundary != null);
            ringIndex++;
        } while (waitingBoundaries.Count > 0);

        List<CellVertex>? maxAreaVertices = null;
        List<CellBoundary>? maxAreaBoundaries = null;
        double maxArea = Double.MinValue;
        for (int i = 0; i < vertices.Count; i++)
        {
            List<CellVertex> tempRing = new List<CellVertex>(vertices[i]);
            tempRing.Add(vertices[i][0]);
            LinearRing ring = new GeometryFactory().CreateLinearRing(tempRing.Select(cv => cv.Coordinate).ToArray());
            if (!ring.IsCCW)
                vertices[i].Reverse();
            double area = Area.OfRing(ring.Coordinates);
            if (maxArea < area)
            {
                maxArea = area;
                maxAreaVertices = vertices[i];
                maxAreaBoundaries = boundaries[i];
            }
        }
        if (maxAreaVertices == null || maxAreaBoundaries == null) throw new Exception("Oops");

        // Add merged hole
        return new CellSpace(maxAreaVertices, maxAreaBoundaries, "generated temp cellspace");
    }

    public void AddHole(CellSpace cellSpace)
    {
        // Add hole into another hole, ignore
        foreach (var hole in Holes)
            if (hole.Polygon.Contains(cellSpace.Geom))
                return;

        // new hole eat some hole
        List<CellSpace> independentHole = new List<CellSpace>();
        foreach (var hole in Holes)
            if (!cellSpace.ShellCellSpace().Polygon.Contains(hole.Geom))
                independentHole.Add(hole);
        Holes = independentHole;

        if (!Polygon.Contains(cellSpace.Polygon.Shell))
        {
            Console.WriteLine(Polygon);
            Console.WriteLine(cellSpace.Polygon.Shell);
        }

        if (!Polygon.Contains(cellSpace.Polygon.Shell)) throw new ArgumentException("the polygon should contain the new hole.");


        Holes.Add(cellSpace.ShellCellSpace());

        HashSet<CellSpace> holesSet = new HashSet<CellSpace>(Holes);

        bool merge;
        do
        {
            merge = false;

            foreach (var hole1 in holesSet)
            {
                foreach (var hole2 in holesSet)
                {
                    if (System.Object.ReferenceEquals(hole1, hole2)) continue;  // generate pair of hole
                    foreach (var b1 in hole1.shellBoundaries)
                    {
                        var b2 = hole2.shellBoundaries.FirstOrDefault(b2 => System.Object.ReferenceEquals(b1, b2));
                        if (b2 == null) continue;

                        // adjacent holes have common boundary, lets merge them
                        merge = true;
                        holesSet.Remove(hole1);
                        holesSet.Remove(hole2);
                        holesSet.Add(Merge(hole1, hole2));
                        goto AFTER_MERGE;
                    }
                }
            }

        AFTER_MERGE:;

        } while (merge);

        Holes.Clear();
        Holes.AddRange(holesSet);

        Geom = new GeometryFactory().CreatePolygon(Polygon.Shell, Holes.Select(h => h.Polygon.Shell).ToArray());
        OnUpdate?.Invoke();
    }

    public CellSpace FindHole(CellSpace cellspace)
    {
        if (Holes.Contains(cellspace))
            return cellspace;
        else
        {
            CellSpace? hole = Holes.FirstOrDefault(hole => hole.Polygon.EqualsTopologically(cellspace.Geom));
            return hole;
        }
    }

    public void RemoveHole(CellSpace cellspace)
    {
        if (Holes.Contains(cellspace))  // Remove whole hole
        {
            Holes.Remove(cellspace);
            Geom = new GeometryFactory().CreatePolygon(Polygon.Shell, Holes.Select(h => h.Polygon.Shell).ToArray());
            OnUpdate?.Invoke();
            return;
        }

        // TODO(naming): this code may work but we should change the name of the method (P.S. we remove the hole contain the argument)
        CellSpace? hole = Holes.FirstOrDefault(hole => hole.Geom!.Contains(cellspace.Geom));
        if (hole == null)
        {
            throw new ArgumentException("No hole contain the hole to be remove");
        }
        else
        {
            Holes.Remove(hole);
            Geom = new GeometryFactory().CreatePolygon(Polygon.Shell, Holes.Select(h => h.Polygon.Shell).ToArray());
            OnUpdate?.Invoke();
            return;
        }
    }

    public List<CellBoundary> InOutBound()
    {
        return allBoundaries.Where(b =>
        {
            if (b.SmartNavigable() != Navigable.Navigable) return false;

            if (b.Another(this) != null && b.NaviDir != NaviDirection.NoneDirection)
                return true;
            else return false;

        }).ToList();
    }

    public List<CellBoundary> InBound()
    {
        return allBoundaries.Where(b =>
        {
            if (b.SmartNavigable() != Navigable.Navigable) return false;
            if (b.leftSpace == this)
            {
                if (b.rightSpace != null)
                    switch (b.NaviDir)
                    {
                        case NaviDirection.Right2Left:
                        case NaviDirection.BiDirection:
                            return true;
                        default:
                            return false;
                    }
                else
                    return false;
            }
            else if (b.rightSpace == this)
            {
                if (b.leftSpace != null)
                    switch (b.NaviDir)
                    {
                        case NaviDirection.Left2Right:
                        case NaviDirection.BiDirection:
                            return true;
                        default:
                            return false;
                    }
                else
                    return false;
            }
            else throw new Exception("space contain the boundary but it is neither the left nor the right side of boundary");
        }).ToList();
    }

    public List<CellBoundary> OutBound()
    {
        return allBoundaries.Where(b =>
        {
            if (b.SmartNavigable() != Navigable.Navigable) return false;
            if (b.leftSpace == this)
            {
                if (b.rightSpace != null)
                    switch (b.NaviDir)
                    {
                        case NaviDirection.Left2Right:
                        case NaviDirection.BiDirection:
                            return true;
                        default:
                            return false;
                    }
                else
                    return false;
            }
            else if (b.rightSpace == this)
            {
                if (b.leftSpace != null)
                    switch (b.NaviDir)
                    {
                        case NaviDirection.Right2Left:
                        case NaviDirection.BiDirection:
                            return true;
                        default:
                            return false;
                    }
                else
                    return false;
            }
            else throw new Exception("space contain the boundary but it is neither the left nor the right side of boundary");
        }).ToList();
    }

    public List<CellBoundary> StopBound()
    {
        return allBoundaries.Where(b =>
        {
            CellSpace? another = b.Another(this);
            if (another == null) return false;
            if (another.navigable == Navigable.PhysicallyNonNavigable && b.Navigable == Navigable.Navigable) return true;
            else return false;
        }).ToList();
    }
}
