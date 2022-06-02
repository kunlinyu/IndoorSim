using System;

#nullable enable

public class AgentDescriptor
{
    public string name = "";
    public string type = "";
    public float x;
    public float y;
    public float theta;
    public string? containerId;  // if containerId != null, them there should be one container with this id in IndoorData.
                                 // And the x, y will be the Centroid of the container.geom

    public AgentDescriptor Clone()
    {
        return new AgentDescriptor() {
            name = name,
            type = type,
            x = x,
            y = y,
            theta = theta,
            containerId = containerId,
        };
    }

    public void CopyFrom(AgentDescriptor agent)
    {
        name = agent.name;
        type = agent.type;
        x = agent.x;
        y = agent.y;
        theta = agent.theta;
        containerId = agent.containerId;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        AgentDescriptor other = (AgentDescriptor)obj;
        return name == other.name && type == other.type && Math.Abs(x - other.x) < 1e-4 && Math.Abs(y - other.y) < 1e-4;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(name, type, x, y, theta, containerId);
    }
}
