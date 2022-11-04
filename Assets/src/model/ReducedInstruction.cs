using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


#nullable enable

public enum Predicate
{
    Add,
    Update,
    Remove,
    Split,
    Merge,
}

public enum SubjectType
{
    Boundary,
    Vertices,
    BoundaryDirection,
    BoundaryNavigable,
    SpaceNavigable,
    RLine,
    SpaceId,

    GripMap,  // TODO(future feature):: create and interpret

    Agent,
    POI,
    Task,  // TODO(future feature):: create and interpret
}

// Following instructions are valid:
// 1. Add Boundary
// 2. Update Boundary
// 3. Remove Boundary
// 4. Update Vertices
// 5. Update boundary direction
// 6. Update boundary navigable
// 7. Update space navigable
// 8. Update RLine PassType
// 9. Update SpaceId
// 10. Add grid map     // TODO
// 11. Update grid map  // TODO
// 12. Remove grid map  // TODO
// 13. Add Agent
// 14. Update Agent  // Doing
// 15. Remove Agent
// 16. Add POI
// 17. Update POI
// 18. Remove POI
// 19. Split Boundary
// 20. Merge Boundary

public class NaviInfo
{
    [JsonPropertyAttribute] public NaviDirection? direction;
    [JsonPropertyAttribute] public Navigable? navigable;
    [JsonPropertyAttribute] public PassType? passType;
}

[Serializable]
public class Parameters
{
    [JsonPropertyAttribute] public Coordinate? coor;
    [JsonPropertyAttribute] public List<Coordinate>? coors;
    [JsonPropertyAttribute] public LineString? lineString;
    [JsonPropertyAttribute] public NaviInfo? naviInfo;
    [JsonPropertyAttribute] public NaviDirection? direction;
    [JsonPropertyAttribute] public Navigable? navigable;
    [JsonPropertyAttribute] public PassType? passType;
    [JsonPropertyAttribute] public Task? task;
    [JsonPropertyAttribute] public AgentDescriptor? agent;
    [JsonPropertyAttribute] public string? containerId;
    [JsonPropertyAttribute] public string? childrenId;
    [JsonPropertyAttribute] public string? value;
    [JsonPropertyAttribute] public List<string>? values;
    [JsonPropertyAttribute] public List<string>? values2;

    public override string ToString()  // TODO: SLOW
        => JsonConvert.SerializeObject(this, new CoorConverter(), new WKTConverter());
}

public static class NavigableExtension
{
    public static NaviDirection direction(this NaviInfo? navi) => navi!.direction!.Value;
    public static Navigable navigable(this NaviInfo? navi) => navi!.navigable!.Value;
    public static PassType passType(this NaviInfo? navi) => navi!.passType!.Value;
}
public static class ParameterExtension
{
    public static Coordinate coor(this Parameters? param) => param!.coor!;
    public static List<Coordinate> coors(this Parameters? param) => param!.coors!;
    public static LineString lineString(this Parameters? param) => param!.lineString!;

    // TODO: make this method simpler after V0.9.0 EOL
    public static NaviDirection direction(this Parameters? param)
    {
        if (param!.direction != null)
            return param!.direction.Value;
        else
            return param!.naviInfo!.direction!.Value;
    }

    // TODO: make this method simpler after V0.9.0 EOL
    public static Navigable navigable(this Parameters? param)
    {
        if (param!.navigable != null)
            return param!.navigable.Value;
        else
            return param!.naviInfo!.navigable!.Value;
    }

    // TODO: make this method simpler after V0.9.0 EOL
    public static PassType passType(this Parameters? param)
    {
        if (param!.passType != null)
            return param!.passType.Value;
        else
            return param!.naviInfo!.passType!.Value;
    }
    public static AgentDescriptor agent(this Parameters? param) => param!.agent!;
    public static Task task(this Parameters? param) => param!.task!;
    public static string containerId(this Parameters? param) => param!.containerId!;
    public static string childrenId(this Parameters? param) => param!.childrenId!;
    public static string value(this Parameters? param) => param!.value!;
    public static List<string> values(this Parameters? param) => param!.values!;
    public static List<string> values2(this Parameters? param) => param!.values2!;
}

[Serializable]
public class ReducedInstruction
{
    [JsonPropertyAttribute] public SubjectType subject { get; set; }
    [JsonPropertyAttribute] public Predicate predicate { get; set; }
    [JsonPropertyAttribute] public Parameters? oldParam { get; set; } = null;
    [JsonPropertyAttribute] public Parameters? newParam { get; set; } = null;
    [JsonPropertyAttribute] public DateTime? dateTime { get; set; } = null;

    ReducedInstruction() { }

    [OnDeserialized]
    void OnReducedInstructionDeserialized(StreamingContext context)
    {
        if (oldParam?.naviInfo != null || newParam?.naviInfo != null)
        {
            switch (subject)
            {
                case SubjectType.BoundaryDirection:
                    oldParam.direction = oldParam.naviInfo.direction;
                    newParam.direction = newParam.naviInfo.direction;
                    break;
                case SubjectType.BoundaryNavigable:
                case SubjectType.SpaceNavigable:
                    oldParam.navigable = oldParam.naviInfo.navigable;
                    newParam.navigable = newParam.naviInfo.navigable;
                    break;
                case SubjectType.RLine:
                    oldParam.passType = oldParam.naviInfo.passType;
                    newParam.passType = newParam.naviInfo.passType;
                    break;
            }
            oldParam.naviInfo = null;
            newParam.naviInfo = null;
        }
    }

    ReducedInstruction(bool nonDeserialization)
    {
        dateTime = DateTime.Now;
    }


    public static LineString Clone(LineString ls)
    {
        Coordinate[] coors = new Coordinate[ls.NumPoints];
        for (int i = 0; i < ls.NumPoints; i++)
            coors[i] = ls.GetCoordinateN(i);
        return new LineString(coors);
    }

    // TODO: SLOW
    public override string ToString()
        => predicate + " " + subject + " " + oldParam?.ToString() + " " + newParam?.ToString();

    public static ReducedInstruction UpdateVertices(List<Coordinate> oldCoors, List<Coordinate> newCoors)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.Vertices;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { coors = oldCoors };
        ri.newParam = new Parameters() { coors = newCoors };
        return ri;
    }

    public static ReducedInstruction AddBoundary(CellBoundary boundary)
        => AddBoundary(boundary.geom);

    public static ReducedInstruction AddBoundary(LineString ls)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Add;
        ri.newParam = new Parameters() { lineString = Clone(ls) };
        return ri;
    }

    public static ReducedInstruction RemoveBoundary(CellBoundary boundary)
        => RemoveBoundary(boundary.geom);

    public static ReducedInstruction RemoveBoundary(LineString ls)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Remove;
        ri.oldParam = new Parameters() { lineString = Clone(ls) };
        return ri;
    }

    public static ReducedInstruction UpdateBoundary(LineString oldLineString, LineString newLineString)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { lineString = Clone(oldLineString) };
        ri.newParam = new Parameters() { lineString = Clone(newLineString) };
        return ri;
    }

    public static ReducedInstruction SplitBoundary(LineString oldLineString, Coordinate middleCoor)
    {
        return new ReducedInstruction(true)
        {
            subject = SubjectType.Boundary,
            predicate = Predicate.Split,
            oldParam = new() { lineString = Clone(oldLineString) },
            newParam = new() { coor = middleCoor },
        };
    }

    public static ReducedInstruction MergeBoundary(LineString newLineString, Coordinate middleCoor)
    {
        return new ReducedInstruction(true)
        {
            subject = SubjectType.Boundary,
            predicate = Predicate.Merge,
            oldParam = new() { coor = middleCoor },
            newParam = new() { lineString = Clone(newLineString) },
        };
    }

    public static ReducedInstruction UpdateBoundaryDirection(LineString oldLineString, NaviDirection oldDirection, NaviDirection newDirection)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.BoundaryDirection;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { lineString = Clone(oldLineString), direction = oldDirection };
        ri.newParam = new Parameters() { direction = newDirection };
        return ri;
    }

    public static ReducedInstruction UpdateBoundaryNavigable(LineString oldLineString, Navigable oldNavigable, Navigable newNavigable)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.BoundaryNavigable;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { lineString = Clone(oldLineString), navigable = oldNavigable };
        ri.newParam = new Parameters() { navigable = newNavigable };
        return ri;
    }

    public static ReducedInstruction UpdateSpaceNavigable(Coordinate spaceInterior, Navigable oldNavigable, Navigable newNavigable)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.SpaceNavigable;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { coor = spaceInterior, navigable = oldNavigable };
        ri.newParam = new Parameters() { navigable = newNavigable };
        return ri;
    }

    public static ReducedInstruction UpdateRLinePassType(LineString oldLineString, PassType oldPassType, PassType newPassType)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.RLine;
        ri.predicate = Predicate.Update;
        ri.oldParam = new Parameters() { lineString = oldLineString, passType = oldPassType };
        ri.newParam = new Parameters() { passType = newPassType };
        return ri;
    }

    public static ReducedInstruction AddAgent(AgentDescriptor agent)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.Agent;
        ri.predicate = Predicate.Add;

        ri.newParam = new Parameters() { agent = agent.Clone() };

        return ri;
    }

    public static ReducedInstruction RemoveAgent(AgentDescriptor agent)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.Agent;
        ri.predicate = Predicate.Remove;

        ri.oldParam = new Parameters() { agent = agent.Clone() };

        return ri;
    }

    public static ReducedInstruction UpdateAgent(AgentDescriptor oldAgent, AgentDescriptor newAgent)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.Agent;
        ri.predicate = Predicate.Update;

        ri.oldParam = new Parameters() { agent = oldAgent.Clone() };
        ri.newParam = new Parameters() { agent = newAgent.Clone() };

        return ri;
    }

    public static ReducedInstruction UpdateSpaceId(Coordinate spaceInterior, string oldContainerId, string oldChildrenId, string newContainerId, string newChildrenId)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.SpaceId;
        ri.predicate = Predicate.Update;

        ri.oldParam = new Parameters() { containerId = oldContainerId, childrenId = oldChildrenId, coor = spaceInterior };
        ri.newParam = new Parameters() { containerId = newContainerId, childrenId = newChildrenId };

        return ri;
    }

    public static ReducedInstruction AddIndoorPOI(Coordinate poiCoor, List<Coordinate> spacesInterior, Coordinate[] queueInterior, List<string> category, List<string> labels)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.POI;
        ri.predicate = Predicate.Add;
        LineString queue = new GeometryFactory().CreateLineString(queueInterior);

        ri.newParam = new Parameters() { coor = poiCoor, coors = spacesInterior, lineString = queue, values = category, values2 = labels };

        return ri;
    }

    public static ReducedInstruction RemoveIndoorPOI(Coordinate poiCoor, List<Coordinate> spacesInterior, Coordinate[] queueInterior, List<string> category, List<string> labels)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.POI;
        ri.predicate = Predicate.Remove;
        LineString queue = new GeometryFactory().CreateLineString(queueInterior);

        ri.oldParam = new Parameters() { coor = poiCoor, coors = spacesInterior, lineString = queue, values = category, values2 = labels };
        return ri;
    }

    public static ReducedInstruction UpdateIndoorPOI(Coordinate oldCoor, Coordinate newCoor)
    {
        ReducedInstruction ri = new(true);
        ri.subject = SubjectType.POI;
        ri.predicate = Predicate.Update;

        ri.oldParam = new Parameters() { coor = oldCoor };
        ri.newParam = new Parameters() { coor = newCoor };
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
                    case Predicate.Split:
                        return MergeBoundary(oldParam.lineString(), newParam.coor());
                    case Predicate.Merge:
                        return SplitBoundary(newParam.lineString(), oldParam.coor());
                    default:
                        throw new ArgumentException("Unknown predicate");
                }
            case SubjectType.BoundaryDirection:
                if (predicate == Predicate.Update)
                    return UpdateBoundaryDirection(oldParam.lineString(), newParam.direction(), oldParam.direction());
                else
                    throw new ArgumentException("boundary direction can only update.");

            case SubjectType.BoundaryNavigable:
                if (predicate == Predicate.Update)
                    return UpdateBoundaryNavigable(oldParam.lineString(), newParam.navigable(), oldParam.navigable());
                else throw new ArgumentException("boundary navigable can only update.");

            case SubjectType.SpaceNavigable:
                if (predicate == Predicate.Update)
                    return UpdateSpaceNavigable(oldParam.coor(), newParam.navigable(), oldParam.navigable());
                else throw new ArgumentException("space navigable can only update.");

            case SubjectType.SpaceId:
                if (predicate == Predicate.Update)
                    return UpdateSpaceId(oldParam.coor(), newParam.containerId(), newParam.childrenId(), oldParam.containerId(), oldParam.childrenId());
                else throw new ArgumentException("space id can only update.");

            case SubjectType.RLine:
                if (predicate == Predicate.Update)
                    return UpdateRLinePassType(oldParam.lineString(), newParam.passType(), oldParam.passType());
                else throw new ArgumentException("rLine pass type can only update.");

            case SubjectType.Agent:
                switch (predicate)
                {
                    case Predicate.Add:
                        return RemoveAgent(newParam.agent());
                    case Predicate.Remove:
                        return AddAgent(oldParam.agent());
                    case Predicate.Update:
                        return UpdateAgent(newParam.agent(), oldParam.agent());
                    default:
                        throw new ArgumentException("Unknown predicate");
                }
            case SubjectType.POI:
                switch (predicate)
                {
                    case Predicate.Add:
                        return RemoveIndoorPOI(newParam.coor(), newParam.coors(), newParam.lineString().Coordinates, newParam.values(), newParam.values2());
                    case Predicate.Update:
                        return UpdateIndoorPOI(newParam.coor(), oldParam.coor());
                    case Predicate.Remove:
                        return AddIndoorPOI(oldParam.coor(), oldParam.coors(), oldParam.lineString().Coordinates, oldParam.values(), oldParam.values2());
                    default:
                        throw new ArgumentException("Unknown predicate");
                }

            default:
                throw new ArgumentException("Unknown subject type");
        }
    }

}
