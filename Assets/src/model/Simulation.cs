using System;
using System.Collections.Generic;

public class Simulation
{
    private ThematicLayer indoorData;
    private SimData simData;
    private List<IActuatorSensor> HWs = null;

    private MapService mapService = null;
    public List<AbstractAgent> agents = new List<AbstractAgent>();
    private TaskAllocator taskAllocator = null;
    private TasksPlayer player = null;

    double startTime = 0.0f;


    public Simulation(ThematicLayer indoorData, SimData simData, List<IActuatorSensor> agentHWs)
    {
        this.indoorData = indoorData;
        this.simData = simData;
        this.HWs = agentHWs;
    }

    private void MapServiceUp()
    {
        mapService = new MapService(indoorData);
    }

    private void AgentsUp()
    {
        if (mapService == null) throw new InvalidOperationException("Up map service first");
        agents.Clear();
        foreach (var agentHW in HWs)
        {
            if (agentHW.AgentDescriptor.type == "capsule")
            {
                if (agentHW.AgentDescriptor.ai == "builtin")
                {
                    var motionExecutor = new TranslateToCoorMotionExecutor(agentHW);
                    var idActionExecutor = new MoveToContainerActionExecutor(motionExecutor, mapService);
                    var coorActionExecutor = new MoveToCoorActionExecutor(motionExecutor);
                    var agent = new DummyAgent(new DummyPlanner(), idActionExecutor, coorActionExecutor);
                    agents.Add(agent);

                    agentHW.motionExecutor = motionExecutor;
                }
                else
                {
                    throw new Exception("unsupported ai : " + agentHW.AgentDescriptor.ai);
                }
            }
            else if (agentHW.AgentDescriptor.type == "boxcapsule")
            {
                if (agentHW.AgentDescriptor.ai == "builtin")
                {
                    var motionExecutor = new TranslateToCoorMotionExecutor(agentHW);
                    var idActionExecutor = new MoveToContainerActionExecutor(motionExecutor, mapService);
                    var coorActionExecutor = new MoveToCoorActionExecutor(motionExecutor);
                    var agent = new DummyAgent(new DummyPlanner(), idActionExecutor, coorActionExecutor);
                    agents.Add(agent);

                    agentHW.motionExecutor = motionExecutor;
                }
                else
                {
                    throw new Exception("unsupported ai : " + agentHW.AgentDescriptor.ai);
                }
            }
            else if (agentHW.AgentDescriptor.type == "bronto")
            {
                if (agentHW.AgentDescriptor.ai == "builtin")
                {
                    var motionExecutor = new TwistToCoorMotionExecutor(agentHW);
                    var idActionExecutor = new MoveToContainerActionExecutor(motionExecutor, mapService);
                    var coorActionExecutor = new MoveToCoorActionExecutor(motionExecutor);
                    var agent = new DummyAgent(new DummyPlanner(), idActionExecutor, coorActionExecutor);
                    agents.Add(agent);

                    agentHW.motionExecutor = motionExecutor;
                }
                else
                {
                    throw new Exception("unsupported ai : " + agentHW.AgentDescriptor.ai);
                }
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

        agents.ForEach(agent => agent.PrepareForReset());
        agents.ForEach(agent => agent.Reset());

        HWs.ForEach(agentHW => agentHW.ResetToInitStatus());
    }

}
