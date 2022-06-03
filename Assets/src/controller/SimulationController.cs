using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public IndoorSimData indoorSimData;
    public SimulationView simulationView;
    public Simulation simulation = null;

    public UIEventDispatcher eventDispatcher;

    private float timeScale = 1.0f;

    void Start()
    {
        eventDispatcher.eventListener += EventListener;
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

                    indoorSimData.currentSimData.tasks.Clear();

                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() {
                        new ActionMoveToCoor(1.0f, 1.0f),
                        new ActionMoveToCoor(-1.0f, 1.0f),
                        new ActionMoveToCoor(-1.0f, -1.0f),
                        new ActionMoveToCoor(1.0f, -1.0f),
                        new ActionMoveToCoor(1.0f, 1.0f),
                    }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(2.0d, new List<AgentAction>() {
                        new ActionMoveToCoor(2.0f, 2.0f),
                        new ActionMoveToCoor(-2.0f, 2.0f),
                        new ActionMoveToCoor(-2.0f, -2.0f),
                        new ActionMoveToCoor(2.0f, -2.0f),
                        new ActionMoveToCoor(2.0f, 2.0f),
                    }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(3.0d, new List<AgentAction>() {
                        new ActionMoveToCoor(3.0f, 3.0f),
                        new ActionMoveToCoor(-3.0f, 3.0f),
                        new ActionMoveToCoor(-3.0f, -3.0f),
                        new ActionMoveToCoor(3.0f, -3.0f),
                        new ActionMoveToCoor(3.0f, 3.0f),
                    }));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(4.0d, new List<AgentAction>() {
                        new ActionMoveToCoor(4.0f, 4.0f),
                        new ActionMoveToCoor(-4.0f, 4.0f),
                        new ActionMoveToCoor(-4.0f, -4.0f),
                        new ActionMoveToCoor(4.0f, -4.0f),
                        new ActionMoveToCoor(4.0f, 4.0f),
                    }));


                    simulation = new Simulation(indoorSimData.indoorData, indoorSimData.currentSimData, simulationView.GetAgentHWs());
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
                simulation?.ResetAll();
                simulation = null;
                Debug.Log($"simulation \"{indoorSimData.currentSimData.name}\" stopped");
            }

            if (e.name == "fast")
            {
                if (simulation != null)
                {
                    if (timeScale >= 1.0f)
                        timeScale += 1.0f;
                    else timeScale *= 2.0f;
                }

                if (Mathf.Abs(timeScale - 1.0f) < 1e-3)
                    timeScale = 1.0f;
                Time.timeScale = timeScale;
                Debug.Log("simulation speed: " + timeScale);
            }

            if (e.name == "slow")
            {
                if (simulation != null)
                {
                    if (timeScale > 1.0f)
                        timeScale -= 1.0f;
                    else timeScale /= 2.0f;
                }

                if (Mathf.Abs(timeScale - 1.0f) < 1e-3)
                    timeScale = 1.0f;
                Time.timeScale = timeScale;
                Debug.Log("simulation speed: " + timeScale);
            }
        }
    }

    void Update()
    {
        simulation?.TikTok(Time.time);
    }
}
