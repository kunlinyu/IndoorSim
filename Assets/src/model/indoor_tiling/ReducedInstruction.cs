using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using UnityEngine;

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

public struct NaviInfo
{
    [JsonPropertyAttribute] public NaviDirection direction;
    [JsonPropertyAttribute] public Navigable navigable;
    [JsonPropertyAttribute] public PassType passType;
}

[Serializable]
public struct Parameters
{
    [JsonPropertyAttribute] public Coordinate? coor;
    [JsonPropertyAttribute] public List<Coordinate>? coors;
    [JsonPropertyAttribute] public LineString? lineString;
    [JsonPropertyAttribute] public NaviInfo? naviInfo;

    public override string ToString()
        => JsonConvert.SerializeObject(this, new CoorConverter(), new WKTConverter());
}

[Serializable]
public struct ParametersC
{
    [JsonPropertyAttribute] public Coordinate? oldCoor;
    [JsonPropertyAttribute] public List<Coordinate>? oldCoors;
    [JsonPropertyAttribute] public LineString? oldLineString;
    [JsonPropertyAttribute] public NaviDirection oldDirection;
    [JsonPropertyAttribute] public Navigable oldNavigable;
    [JsonPropertyAttribute] public PassType oldPassType;

    [JsonPropertyAttribute] public Coordinate? newCoor;
    [JsonPropertyAttribute] public List<Coordinate>? newCoors;
    [JsonPropertyAttribute] public LineString? newLineString;
    [JsonPropertyAttribute] public NaviDirection newDirection;
    [JsonPropertyAttribute] public Navigable newNavigable;
    [JsonPropertyAttribute] public PassType newPassType;


    public override string ToString()
        => JsonConvert.SerializeObject(this, new CoorConverter(), new WKTConverter());
}

public static class ParameterExtension
{
    public static Coordinate coor(this Parameters? param) => param!.Value.coor!;
    public static List<Coordinate> coors(this Parameters? param) => param!.Value.coors!;
    public static LineString lineString(this Parameters? param) => param!.Value.lineString!;
    public static NaviInfo naviInfo(this Parameters? param) => param!.Value.naviInfo!.Value;
}

[Serializable]
public class ReducedInstruction
{
    [JsonPropertyAttribute] public SubjectType subject { get; set; }
    [JsonPropertyAttribute] public Predicate predicate { get; set; }
    [JsonPropertyAttribute] public Parameters? oldParam { get; set; } = null;
    [JsonPropertyAttribute] public Parameters? newParam { get; set; } = null;

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
        => predicate + " " + subject + " " + oldParam.ToString() + " " + newParam.ToString();

    public static ReducedInstruction UpdateVertices(List<Coordinate> oldCoors, List<Coordinate> newCoors)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Vertices;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { coors = oldCoors };
        ri.newParam = new Parameters() { coors = newCoors };
        return ri;
    }

    public static ReducedInstruction AddBoundary(CellBoundary boundary)
        => AddBoundary(boundary.Geom);

    public static ReducedInstruction AddBoundary(LineString ls)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Add;
        ri.newParam = new Parameters() { lineString = Clone(ls) };
        return ri;
    }

    public static ReducedInstruction RemoveBoundary(CellBoundary boundary)
        => RemoveBoundary(boundary.Geom);

    public static ReducedInstruction RemoveBoundary(LineString ls)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Remove;
        ri.oldParam = new Parameters() { lineString = Clone(ls) };
        return ri;
    }

    public static ReducedInstruction UpdateBoundary(LineString oldLineString, LineString newLineString)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { lineString = Clone(oldLineString) };
        ri.newParam = new Parameters() { lineString = Clone(newLineString) };
        return ri;
    }

    public static ReducedInstruction UpdateBoundaryDirection(LineString oldLineString, NaviDirection oldDirection, NaviDirection newDirection)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.BoundaryDirection;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { lineString = Clone(oldLineString), naviInfo = new NaviInfo() { direction = oldDirection } };
        ri.newParam = new Parameters() { naviInfo = new NaviInfo() { direction = newDirection } };
        return ri;
    }

    public static ReducedInstruction UpdateSpaceNavigable(Coordinate spaceInterior, Navigable oldNavigable, Navigable newNavigable)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.SpaceNavigable;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { coor = spaceInterior, naviInfo = new NaviInfo() { navigable = oldNavigable } };
        ri.newParam = new Parameters() { naviInfo = new NaviInfo() { navigable = newNavigable } };
        return ri;
    }

    public static ReducedInstruction UpdateRLinePassType(LineString oldLineString, PassType oldPassType, PassType newPassType)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.RLine;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { lineString = oldLineString, naviInfo = new NaviInfo() { passType = oldPassType } };
        ri.newParam = new Parameters() { naviInfo = new NaviInfo() { passType = newPassType } };
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
                        return UpdateVertices(newParam.coors(), oldParam.coors());
                    default:
                        throw new ArgumentException("Unknown predicate");
                }
            case SubjectType.Boundary:
                switch (predicate)
                {
                    case Predicate.Add:
                        return RemoveBoundary(newParam.lineString());
                    case Predicate.Remove:
                        return AddBoundary(oldParam.lineString());
                    case Predicate.Update:
                        return UpdateBoundary(newParam.lineString(), oldParam.lineString());
                    default:
                        throw new ArgumentException("Unknown predicate");
                }
            case SubjectType.BoundaryDirection:
                if (predicate == Predicate.Update)
                    return UpdateBoundaryDirection(oldParam.lineString(), newParam.naviInfo().direction, oldParam.naviInfo().direction);
                else
                    throw new ArgumentException("boundary direction can only update.");
            case SubjectType.SpaceNavigable:
                if (predicate == Predicate.Update)
                    return UpdateSpaceNavigable(oldParam.coor(), newParam.naviInfo().navigable, oldParam.naviInfo().navigable);
                else throw new ArgumentException("space navigable can only update.");
            case SubjectType.RLine:
                if (predicate == Predicate.Update)
                    return UpdateRLinePassType(oldParam.lineString(), newParam.naviInfo().passType, oldParam.naviInfo().passType);
                else throw new ArgumentException("rLine pass type can only update.");
            default:
                throw new ArgumentException("Unknown subject type");
        }
    }

}
