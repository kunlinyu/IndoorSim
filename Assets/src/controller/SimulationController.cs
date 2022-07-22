using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json.Linq;

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

                    indoorSimData.simulating = true;
                    indoorSimData.currentSimData.tasks.Clear();

                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("0")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("1")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("2")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("3")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("4")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("5")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("6")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("7")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("8")}));
                    indoorSimData.currentSimData.tasks.Add(new ActionListTask(1.0d, new List<AgentAction>() { new ActionMoveToContainer("9")}));

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
                indoorSimData.simulating = false;
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
        else if (e.type == UIEventType.Resources)
        {
            if (e.name == "gridmap")
            {

                Debug.Log("controller get gridmap");
                GridMap gridMap = new GridMap();

                var jsonData = JObject.Parse(e.message);
                gridMap.id = jsonData["id"].Value<string>();
                gridMap.resolution = jsonData["resolution"].Value<double>();
                gridMap.imageBase64 = jsonData["base64Content"].Value<string>();
                gridMap.localOrigin.x = jsonData["origin_x"].Value<double>();
                gridMap.localOrigin.y = jsonData["origin_y"].Value<double>();
                gridMap.localOrigin.theta = jsonData["origin_theta"].Value<double>();

                indoorSimData.AddGridMap(gridMap);
            }
        }
    }

    void Update()
    {
        simulation?.TikTok(Time.time);
    }
}
