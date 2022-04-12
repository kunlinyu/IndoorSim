using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

public enum Predicate
{
    Add,
    Move,
    Remove
}

public enum SubjectType
{
    Vertex,
    Boundary,
}

public struct Parameters
{
    public Coordinate coordinate { get; set; }
    public string id0 { get; set; }
    public string id1 { get; set; }
}

public class ReducedInstruction
{
    public SubjectType subject { get; set; }
    public Predicate predicate { get; set; }
    public Parameters param { get; set; } = new Parameters();
    public Parameters preCond { get; set; } = new Parameters();
    public Parameters postCond { get; set; } = new Parameters();

    static ReducedInstruction AddVertex(CellVertex vertex)
    {
        return AddVertex(vertex.Id, vertex.Coordinate);
    }

    static ReducedInstruction AddVertex(string id, Coordinate coor)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Vertex;
        ri.predicate = Predicate.Add;
        ri.param = new Parameters() { coordinate = coor };
        ri.postCond = new Parameters() { id0 = id };
        return ri;
    }

    static ReducedInstruction RemoveVertex(CellVertex vertex)
    {
        return RemoveVertex(vertex.Id, vertex.Coordinate);
    }

    static ReducedInstruction RemoveVertex(string id, Coordinate coor)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Vertex;
        ri.predicate = Predicate.Remove;
        ri.param = new Parameters() { id0 = id };
        ri.preCond = new Parameters() { coordinate = coor };
        return ri;
    }


    static ReducedInstruction MoveVertex(string id, Coordinate preCoor, Coordinate postCoor)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Vertex;
        ri.predicate = Predicate.Move;
        ri.param = new Parameters() { id0 = id };
        ri.preCond = new Parameters() { coordinate = preCoor };
        ri.postCond = new Parameters() { coordinate = postCoor };
        return ri;
    }

    static ReducedInstruction AddBoundary(CellBoundary boundary)
    {
        return AddBoundary(boundary.P0.Id, boundary.P1.Id, boundary.Id);
    }

    static ReducedInstruction AddBoundary(string vertexId0, string vertexId1, string boundaryId)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Add;
        ri.param = new Parameters() { id0 = vertexId0, id1 = vertexId1 };
        ri.postCond = new Parameters() { id0 = boundaryId };
        return ri;
    }

    static ReducedInstruction RemoveBoundary(CellBoundary boundary)
    {
        return RemoveBoundary(boundary.P0.Id, boundary.P1.Id, boundary.Id);
    }

    static ReducedInstruction RemoveBoundary(string vertexId0, string vertexId1, string boundaryId)
    {
        ReducedInstruction ri = new ReducedInstruction();
        ri.subject = SubjectType.Boundary;
        ri.predicate = Predicate.Remove;
        ri.param = new Parameters() { id0 = boundaryId };
        ri.preCond = new Parameters() { id0 = vertexId0, id1 = vertexId1 };
        return ri;
    }

    public ReducedInstruction Reverse()
    {
        switch (subject)
        {
            case SubjectType.Vertex:
                switch (predicate)
                {
                    case Predicate.Add:
                        return RemoveVertex(postCond.id0, param.coordinate);
                    case Predicate.Remove:
                        return AddVertex(param.id0, preCond.coordinate);
                    case Predicate.Move:
                        return MoveVertex(param.id0, postCond.coordinate, preCond.coordinate);
                    default:
                        throw new InvalidCastException("Unknown predicate");
                }
            case SubjectType.Boundary:
                switch (predicate)
                {
                    case Predicate.Add:
                        return RemoveBoundary(param.id0, param.id1, postCond.id0);
                    case Predicate.Remove:
                        return AddBoundary(preCond.id0, preCond.id1, param.id0);
                    case Predicate.Move:
                        throw new InvalidOperationException("No move boundary instruction");
                    default:
                        throw new InvalidCastException("Unknown predicate");
                }
            default:
                throw new InvalidCastException("Unknown subject type");
        }
    }

}
