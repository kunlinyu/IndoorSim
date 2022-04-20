using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

#nullable enable

public enum Predicate
{
    Add,
    Update,
    Remove
}

public enum SubjectType
{
    Boundary,
    Vertices,
    // TODO: vertices
}

// Following instructions are valid:
// 1. Add Boundary
// 2. Update Boundary
// 3. Remove Boundary
// 4. Update Vertices

[Serializable]
public struct Parameters
{
    [JsonPropertyAttribute] public Coordinate? newCoor;
    [JsonPropertyAttribute] public Coordinate? oldCoor;
    [JsonPropertyAttribute] public List<Coordinate>? oldCoors;
    [JsonPropertyAttribute] public List<Coordinate>? newCoors;
    [JsonPropertyAttribute] public LineString? newLineString;
    [JsonPropertyAttribute] public LineString? oldLineString;

    public override string ToString()
        => JsonConvert.SerializeObject(this, new CoorConverter(), new WKTConverter());
}


[Serializable]
public class ReducedInstruction
{
    [JsonPropertyAttribute] public SubjectType subject { get; set; }
    [JsonPropertyAttribute] public Predicate predicate { get; set; }
    [JsonPropertyAttribute] public Parameters param { get; set; } = new Parameters();

    ReducedInstruction()
    { }

    public static LineString Clone(LineString ls)
    {
        Coordinate[] coors = new Coordinate[ls.NumPoints];
        for (int i = 0; i < ls.NumPoints; i++)
            coors[i] = ls.GetCoordinateN(i);
        return new LineString(coors);
    }

    public override string ToString()
        => predicate + " " + subject + " " + param.ToString();

    public static ReducedInstruction UpdateVertices(List<Coordinate> oldCoors, List<Coordinate> newCoors)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Vertices;
        ri.predicate = Predicate.Update;
        ri.param = new Parameters() { oldCoors = oldCoors, newCoors = newCoors };
        return ri;
    }

    public static ReducedInstruction AddBoundary(CellBoundary boundary)
        => AddBoundary(boundary.Geom);

    public static ReducedInstruction AddBoundary(LineString ls)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Add;
        ri.param = new Parameters() { newLineString = Clone(ls) };
        return ri;
    }

    public static ReducedInstruction RemoveBoundary(CellBoundary boundary)
        => RemoveBoundary(boundary.Geom);

    public static ReducedInstruction RemoveBoundary(LineString ls)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Remove;
        ri.param = new Parameters() { oldLineString = Clone(ls) };
        return ri;
    }

    public static ReducedInstruction UpdateBoundary(LineString oldLineString, LineString newLineString)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Update;
        ri.param = new Parameters() { oldLineString = Clone(oldLineString), newLineString = Clone(newLineString) };
        return ri;
    }

    static public List<ReducedInstruction> Reverse(List<ReducedInstruction> instructions)
    {
        var result = new List<ReducedInstruction>();
        for (int i = instructions.Count - 1; i >= 0; i--)
            result.Add(instructions[i].Reverse());
        return result;
    }

    public ReducedInstruction Reverse()
    {
        switch (subject)
        {
            case SubjectType.Vertices:
                switch (predicate)
                {
                    case Predicate.Update:
                        return UpdateVertices(param.newCoors, param.oldCoors);
                    default:
                        throw new InvalidCastException("Unknown predicate");
                }
            case SubjectType.Boundary:
                switch (predicate)
                {
                    case Predicate.Add:
                        return RemoveBoundary(param.newLineString);
                    case Predicate.Remove:
                        return AddBoundary(param.oldLineString);
                    case Predicate.Update:
                        return UpdateBoundary(param.newLineString, param.oldLineString);
                    default:
                        throw new InvalidCastException("Unknown predicate");
                }
            default:
                throw new InvalidCastException("Unknown subject type");
        }
    }

}
