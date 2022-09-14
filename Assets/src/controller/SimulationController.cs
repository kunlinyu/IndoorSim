using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public IndoorSimData indoorSimData;
    public SimulationView simulationView;
    public Simulation simulation = null;

    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;

    private float timeScale = 1.0f;

    void Start()
    {
        eventSubscriber = new UIEventSubscriber(eventDispatcher);
    }

    void Update()
    {
        eventSubscriber.ConsumeAll(EventListener);
        simulation?.TikTok(Time.time);
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.Simulation)
        {
            if (e.name == "play pause")
            {
                if (simulation == null)  // play
                {
                    if (indoorSimData.currentSimData == null)
                    {
                        Debug.LogWarning("not select one simulation");
                        return;
                    }

                    indoorSimData.simulating = true;
                    indoorSimData.currentSimData.tasks.Clear();

                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("0") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("1") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("2") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("3") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("4") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("5") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("6") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("7") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("8") }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("9") }));

                    // indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() {
                    //     new ActionMoveToCoor(1.0f, 1.0f),
                    //     new ActionMoveToCoor(-1.0f, 1.0f),
                    //     new ActionMoveToCoor(1.0f, -1.0f),
                    //     new ActionMoveToCoor(-1.0f, -1.0f),
                    //     new ActionMoveToCoor(1.0f, 1.0f),
                    // }));
                    // indoorSimData.currentSimData.tasks.Add(new ActionListTask(2.0d, new List<AgentAction>() {
                    //     new ActionMoveToCoor(2.0f, 2.0f),
                    //     new ActionMoveToCoor(-2.0f, 2.0f),
                    //     new ActionMoveToCoor(2.0f, -2.0f),
                    //     new ActionMoveToCoor(-2.0f, -2.0f),
                    //     new ActionMoveToCoor(2.0f, 2.0f),
                    // }));
                    // indoorSimData.currentSimData.tasks.Add(new ActionListTask(3.0d, new List<AgentAction>() {
                    //     new ActionMoveToCoor(3.0f, 3.0f),
                    //     new ActionMoveToCoor(-3.0f, 3.0f),
                    //     new ActionMoveToCoor(-3.0f, -3.0f),
                    //     new ActionMoveToCoor(3.0f, -3.0f),
                    //     new ActionMoveToCoor(3.0f, 3.0f),
                    // }));
                    // indoorSimData.currentSimData.tasks.Add(new ActionListTask(4.0d, new List<AgentAction>() {
                    //     new ActionMoveToCoor(4.0f, 4.0f),
                    //     new ActionMoveToCoor(4.0f, -4.0f),
                    //     new ActionMoveToCoor(-4.0f, -4.0f),
                    //     new ActionMoveToCoor(-4.0f, 4.0f),
                    //     new ActionMoveToCoor(4.0f, 4.0f),
                    // }));

                    simulation = new Simulation(indoorSimData.indoorFeatures.activeLayer, indoorSimData.currentSimData, simulationView.GetAgentHWs());
                    timeScale = 1.0f;
                    Time.timeScale = timeScale;
                    simulation.UpAll(Time.time);

                    Debug.Log($"simulation \"{indoorSimData.currentSimData.name}\" up all services");
                }
                else if (Time.timeScale == 0.0f)  // continue
                {
                    Time.timeScale = timeScale;
                    Debug.Log($"simulation \"{indoorSimData.currentSimData.name}\" paused");
                }
                else  // pause
                {
                    Time.timeScale = 0.0f;
                    Debug.Log($"simulation \"{indoorSimData.currentSimData.name}\" continue");
                }
            }

            if (e.name == "stop")
            {
                if (simulation != null)
                {
                    simulation?.ResetAll();
                    simulation = null;
                    indoorSimData.simulating = false;
                    timeScale = 1.0f;
                    Time.timeScale = timeScale;
                    Debug.Log($"simulation \"{indoorSimData.currentSimData.name}\" stopped");
                }
                else
                {
                    Debug.LogWarning("No simulation is running");
                }
            }

            if (e.name == "fast")
            {
                if (simulation != null)
                {
                    if (timeScale >= 1.0f)
                        timeScale += 1.0f;
                    else timeScale *= 2.0f;
                    if (Mathf.Abs(timeScale - 1.0f) < 1e-3)
                        timeScale = 1.0f;
                    Time.timeScale = timeScale;
                    Debug.Log("simulation speed: " + timeScale);
                }
                else
                {
                    Debug.LogWarning("No simulation is running");
                }
            }

            if (e.name == "slow")
            {
                if (simulation != null)
                {
                    if (timeScale > 1.0f)
                        timeScale -= 1.0f;
                    else timeScale /= 2.0f;
                    if (Mathf.Abs(timeScale - 1.0f) < 1e-3)
                        timeScale = 1.0f;
                    Time.timeScale = timeScale;
                    Debug.Log("simulation speed: " + timeScale);
                }
                else
                {
                    Debug.LogWarning("No simulation is running");
                }
            }
        }
    }
}
