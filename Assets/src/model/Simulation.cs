using System;
using System.Collections.Generic;

public class Simulation
{
    private IndoorData indoorData;
    private SimData simData;
    private List<IAgentHW> agentHWs = null;

    private MapService mapService = null;
    private List<IAgent> agents = new List<IAgent>();
    private TaskAllocator taskAllocator = null;
    private TasksPlayer player = null;

    double startTime = 0.0f;


    public Simulation(IndoorData indoorData, SimData simData, List<IAgentHW> agentHWs)
    {
        this.indoorData = indoorData;
        this.simData = simData;
        this.agentHWs = agentHWs;
    }

    private void MapServiceUp()
    {
        mapService = new MapService(indoorData);
    }

    private void AgentsUp()
    {
        if (mapService == null) throw new InvalidOperationException("Up map service first");
        agents.Clear();
        foreach (var agentHW in agentHWs)
        {
            if (agentHW.AgentDescriptor.type == "capsule")
            {
                var actionExecutor = new IdCoorActionExecutor(agentHW, mapService);
                var QueueCachedActionExecutor = new QueuedCachedExecutor<AgentAction, object, object, ActionExecutorStatus>(actionExecutor);
                var agent = new TaskPlanningAgent(new DummyPlanner(), QueueCachedActionExecutor);
                agents.Add(agent);
            }
            else if (agentHW.AgentDescriptor.type == "boxcapsule")
            {
                var actionExecutor = new IdCoorActionExecutor(agentHW, mapService);
                var QueueCachedActionExecutor = new QueuedCachedExecutor<AgentAction, object, object, ActionExecutorStatus>(actionExecutor);
                var agent = new TaskPlanningAgent(new DummyPlanner(), QueueCachedActionExecutor);
                agents.Add(agent);
            }
            else
            {
                throw new ArgumentException("unsupported agent type: " + agentHW.AgentDescriptor.type);
            }
        }

    }

    private void TaskPlayerAllocatorUp(double startTime)
    {
        taskAllocator = new TaskAllocator(agents);
        taskAllocator.Start();

        player = new TasksPlayer(simData.tasks, taskAllocator.AddTask);
        player.Reset(startTime);

        this.startTime = startTime;
    }

    public void TikTok(double currentTime)
    {
        player.TikTok(currentTime);
    }

    public void UpAll(double startTime)
    {
        MapServiceUp();
        AgentsUp();
        TaskPlayerAllocatorUp(startTime);
    }

    public void ResetAll()
    {
        player.Reset(0.0d);
        taskAllocator.Stop();
        agents.ForEach(agent => agent.AbortAll());
        agentHWs.ForEach(agentHW => agentHW.ResetToInitStatus());
    }

}
