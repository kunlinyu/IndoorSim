using System.Collections;
using System.Collections.Generic;

public class Simulation
{
    private IndoorData indoorData;
    private SimData simData;


    public Simulation(IndoorData indoorData, SimData simData)
    {
        this.indoorData = indoorData;
        this.simData = simData;
    }

    public void Bind(AgentDescriptor agentDescriptor, IAgentHW agentHw)
    {

    }

}
