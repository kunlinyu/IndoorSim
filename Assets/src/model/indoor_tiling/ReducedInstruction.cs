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
    BoundaryDirection,
    SpaceNavigable,
    RLine,
}

// Following instructions are valid:
// 1. Add Boundary
// 2. Update Boundary
// 3. Remove Boundary
// 4. Update Vertices
// 5. Update boundary direction
// 6. Update space navigable
// 7. Update RLine PassType

[Serializable]
public struct Parameters
{
    [JsonPropertyAttribute] public Coordinate? newCoor;
    [JsonPropertyAttribute] public Coordinate? oldCoor;
    [JsonPropertyAttribute] public List<Coordinate>? oldCoors;
    [JsonPropertyAttribute] public List<Coordinate>? newCoors;
    [JsonPropertyAttribute] public LineString? newLineString;
    [JsonPropertyAttribute] public LineString? oldLineString;
    [JsonPropertyAttribute] public NaviDirection oldDirection;
    [JsonPropertyAttribute] public NaviDirection newDirection;
    [JsonPropertyAttribute] public Navigable oldNavigable;
    [JsonPropertyAttribute] public Navigable newNavigable;
    [JsonPropertyAttribute] public PassType oldPassType;
    [JsonPropertyAttribute] public PassType newPassType;

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

    public static ReducedInstruction UpdateBoundaryDirection(LineString oldLineString, NaviDirection oldDirection, NaviDirection newDirection)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.BoundaryDirection;
        ri.predicate = Predicate.Update;
        ri.param = new Parameters() { oldLineString = Clone(oldLineString), oldDirection = oldDirection, newDirection = newDirection };
        return ri;
    }

    public static ReducedInstruction UpdateSpaceNavigable(Coordinate spaceInterior, Navigable oldNavigable, Navigable newNavigable)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.SpaceNavigable;
        ri.predicate = Predicate.Update;
        ri.param = new Parameters() { oldCoor = spaceInterior, oldNavigable = oldNavigable, newNavigable = newNavigable };
        return ri;
    }

    public static ReducedInstruction UpdateRLinePassType(LineString oldLineString, PassType oldPassType, PassType newPassType)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.RLine;
        ri.predicate = Predicate.Update;
        ri.param = new Parameters() { oldLineString = oldLineString, oldPassType = oldPassType, newPassType = newPassType };
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
                        throw new ArgumentException("Unknown predicate");
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
                        throw new ArgumentException("Unknown predicate");
                }
            case SubjectType.BoundaryDirection:
                if (predicate == Predicate.Update)
                    return UpdateBoundaryDirection(param.oldLineString, param.newDirection, param.oldDirection);
                else
                    throw new ArgumentException("boundary direction can only update.");
            case SubjectType.SpaceNavigable:
                if (predicate == Predicate.Update)
                    return UpdateSpaceNavigable(param.oldCoor, param.newNavigable, param.oldNavigable);
                else throw new ArgumentException("space navigable can only update.");
            case SubjectType.RLine:
                if (predicate == Predicate.Update)
                    return UpdateRLinePassType(param.oldLineString, param.newPassType, param.oldPassType);
                else throw new ArgumentException("rLine pass type can only update.");
            default:
                throw new ArgumentException("Unknown subject type");
        }
    }

}
