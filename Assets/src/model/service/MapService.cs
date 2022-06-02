using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

#nullable enable

public class MapService
{
    private IndoorData indoorData;
    public MapService(IndoorData indoorData)
    {
        this.indoorData = indoorData;
    }

    public PlanResult? Path(CoorToContainerQuery query)
    {
        CellSpace? current = FindContainerGeom(new Coordinate(query.x, query.y));
        CellSpace? target = FindContainerId(query.targetContainerId);
        if (current != null && target != null)
        {
            IndoorDataAStar AStar = new IndoorDataAStar(indoorData);
            return AStar.Search(current, target);
        }
        else
        {
            return null;
        }
    }

    public PlanSimpleResult? PathSimple(CoorToContainerQuery query)
        => Path(query)?.ToSimple();

    public CellSpace? FindContainerGeom(Coordinate currentCoor)
        => indoorData.FindSpaceGeom(currentCoor);

    public CellSpace? FindContainerId(string id)
        => indoorData.FindSpaceId(id);

}