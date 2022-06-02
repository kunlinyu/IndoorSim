using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationController : MonoBehaviour
{
    public IndoorSimData indoorSimData;
    public SimulationView simulationView;
    public Simulation simulation;

    public UIEventDispatcher eventDispatcher;

    public float timeScale = 1.0f;


    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.Simulation)
        {
            if (e.message == "play")
            {
                if (indoorSimData.currentSimData == null)
                {
                    Debug.LogWarning("not select one simulation");
                    return;
                }
                simulation = new Simulation(indoorSimData.indoorData, indoorSimData.currentSimData, simulationView.GetAgentHWs());
                timeScale = 1.0f;
                Time.timeScale = timeScale;
                simulation.UpAll(Time.time);
            }

            if (e.message == "pause")
                Time.timeScale = 0.0f;

            if (e.message == "continue")
                Time.timeScale = timeScale;

            if (e.message == "stop")
                simulation.ResetAll();

            if (e.message == "speed up")
            {
                if (timeScale >= 1.0f)
                    timeScale += 1.0f;
                else timeScale *= 2.0f;

                if (Mathf.Abs(timeScale - 1.0f) < 1e-3)
                    timeScale = 1.0f;
                Time.timeScale = timeScale;
            }

            if (e.message == "speed down")
            {
                if (timeScale > 1.0f)
                    timeScale -= 1.0f;
                else timeScale /= 2.0f;

                if (Mathf.Abs(timeScale - 1.0f) < 1e-3)
                    timeScale = 1.0f;
                Time.timeScale = timeScale;
            }
        }
    }

    void Start()
    {
        eventDispatcher.eventListener += EventListener;
    }

    void Update()
    {
        simulation?.TikTok(Time.time);
    }
}
