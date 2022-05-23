using System.Collections;
using System.Collections.Generic;

#nullable enable
public struct AgentDescriptor
{
    public string name;
    public string type;
    public double x;
    public double y;
    public double theta;
    public string? containerId;  // if containerId != null, them there should be one container with this id in IndoorData.
                                 // And the x, y will be the Centroid of the container.geom
}
