using System.Collections;
using System.Collections.Generic;

public class AgentDescriptor
{
    public string name;
    public string type;
}

public class AgentPoseDescriptor : AgentDescriptor
{
    public double x;
    public double y;
    public double theta;
}

public class AgentContainerDescriptor : AgentDescriptor
{
    public string containerId;
    public double theta;
}