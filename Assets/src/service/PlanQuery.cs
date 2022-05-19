using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

public class PlanQuery
{
    public Coordinate sourceCoordinate;  // source description is a joint state. coordinate may generate a source description
    public string targetContainerId;     // TODO: target description is a joint state. We support target container id currently

    // TODO: domain description(the default domain description is: all navigable area)

}
