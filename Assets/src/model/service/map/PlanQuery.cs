using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

public class CoorToContainerQuery
{
    // source description is a joint state. coordinate may generate a source description
    public double x;
    public double y;

    // TODO: target description is a joint state. We support target container id currently
    public string targetContainerId;

    // TODO: domain description(the default domain description is: all navigable area)
}

public class Container2ContainerQuery
{
    public string sourceContainerId;
    public string targetContainerId;

    // TODO: domain description(the default domain description is: all navigable area)
}
