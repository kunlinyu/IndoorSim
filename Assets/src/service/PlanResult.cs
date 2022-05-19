using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;


#nullable enable

public struct SBPair
{
    public Container space;
    public LineString boundary;
    public LineString? representativeLine;
}

public class PlanSimpleResult
{
    public List<Point> boundaryCentroids = new List<Point>();
}


// plan result is a sequence of S(space) and B(boundary):
// S0 -> B0 -> S1 -> B1 -> S2 -> B2 -> S3
// The first S0 contains (geometry.Contains) the source Coordinate
// The last S3 contains (topology.Contains) the target container Id
// The last S3 may be non-navigable beside all others a navigable
public class PlanResult : IEnumerable
{
    public List<SBPair> SBSequence = new List<SBPair>();
    public Container target;

    public PlanResult(Container target)
    {
        this.target = target;
    }

    public IEnumerator GetEnumerator() => SBSequence.GetEnumerator();

    public PlanSimpleResult ToSimple()
        => new PlanSimpleResult() { boundaryCentroids = SBSequence.Select(sbPair => sbPair.boundary.Centroid).ToList() };
}
