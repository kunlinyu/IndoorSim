using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

#nullable enable


public class IndoorDataAdjacentFinder : AdjacentFinder<CellBoundary>
{
    private IndoorData indoorData;

    public IndoorDataAdjacentFinder(IndoorData indoorData)
    {
        this.indoorData = indoorData;
    }

    public List<NodeWithCost<CellBoundary>> adjacentWithCost(CellBoundary boundary, CellBoundary? predecessor)
    {
        return boundary.Spaces().Select(space => space.rLines!)  // get rLines
                       .Select(rls => rls.next(boundary)).SelectMany(bg => bg) // get next BoundaryWithGeoms
                       .Select(bg => new NodeWithCost<CellBoundary>(bg.boundary, bg.rLineGeom.Length))
                       .ToList();
    }
}

public class IndoorTSNodeBreaker : NodeBreaker<CellBoundary>
{
    private CellSpace target;
    public IndoorTSNodeBreaker(CellSpace target) => this.target = target;
    public bool shouldBreak(CellBoundary currentBoundary, double cost, int exploredCount)
        => target.allBoundaries.Contains(currentBoundary);
}

public class IndoorDataAStar
{
    IndoorData indoorTS;
    IndoorDataAdjacentFinder adjacentFinder;

    public IndoorDataAStar(IndoorData indoorData)
    {
        this.indoorTS = indoorData;
        adjacentFinder = new IndoorDataAdjacentFinder(indoorData);
    }

    public PlanResult Search(CellSpace sourceSpace, CellSpace targetSpace)
    {
        IndoorTSNodeBreaker breaker = new IndoorTSNodeBreaker(targetSpace);

        List<CellBoundary> initNodes = sourceSpace.OutBound();

        List<CellBoundary> resultBoundaries =
            AStar<CellBoundary, IndoorDataAdjacentFinder, IndoorTSNodeBreaker>.searchMultiInit(initNodes, adjacentFinder, (node) => { }, breaker);

        if (resultBoundaries.Count > 0)
        {
            PlanResult result = new PlanResult(targetSpace);

            CellSpace lastSpace = sourceSpace;
            for (int i = 0; i < resultBoundaries.Count; i++)
            {
                if (i == 0)
                {
                    result.SBSequence.Add(new SBPair() { space = sourceSpace, boundary = resultBoundaries[i].geom });
                }
                else
                {
                    CellSpace space = resultBoundaries[i - 1].Another(lastSpace)!;
                    result.SBSequence.Add(new SBPair() { space = space, boundary = resultBoundaries[i].geom });
                    lastSpace = space;
                }
            }

            return result;
        }
        else
        {
            return new PlanResult(targetSpace);
        }
    }

}