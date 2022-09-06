using NetTopologySuite.Geometries;

#nullable enable

public class MapService
{
    private ThematicLayer indoorData;
    public MapService(ThematicLayer indoorData)
    {
        this.indoorData = indoorData;
    }

    public PlanResult? Path(CoorToContainerQuery query)
    {
        CellSpace? current = FindContainerGeom(new Coordinate(query.x, query.y));
        CellSpace? target = FindContainerId(query.targetContainerId);
        if (current != null && target != null)
            return new IndoorDataAStar(indoorData).Search(new Coordinate(query.x, query.y), target);
        else
            return null;
    }

    public PlanSimpleResult? PathSimple(CoorToContainerQuery query)
        => Path(query)?.ToSimple();

    public CellSpace? FindContainerGeom(Coordinate currentCoor)
        => indoorData.FindSpaceGeom(currentCoor);

    public CellSpace? FindContainerId(string id)
        => indoorData.FindContainerId(id);

}
